using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpenPc.Infrastructure.Persistence;
using Quartz;

namespace OpenPc.Scraper.Jobs;

/// <summary>
/// Agenda um job Quartz por ScrapeJob habilitado, usando a cron da própria
/// linha (o banco é a fonte de verdade do agendamento).
/// </summary>
public sealed class ScrapeScheduler(
    ISchedulerFactory factory,
    AppDbContext db,
    ILogger<ScrapeScheduler> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        var jobs = await db.ScrapeJobs
            .Where(j => j.Enabled)
            .Include(j => j.Store).Include(j => j.Category)
            .ToListAsync(ct);

        var scheduler = await factory.GetScheduler(ct);

        // Job fixo de agregação diária de preços (M6) — não é scraping.
        await scheduler.ScheduleJob(
            JobBuilder.Create<PriceAggregationJob>()
                .WithIdentity("price-aggregation")
                .Build(),
            TriggerBuilder.Create()
                .WithIdentity("price-aggregation-trigger")
                .WithCronSchedule(PriceAggregationJob.Cron)
                .Build(),
            ct);

        foreach (var job in jobs)
        {
            var key = new JobKey($"job-{job.Id:N}");
            var detail = JobBuilder.Create<ScrapeJobRunner>()
                .WithIdentity(key)
                .UsingJobData("jobId", job.Id.ToString())
                .Build();
            var trigger = TriggerBuilder.Create()
                .WithIdentity($"trigger-{job.Id:N}")
                .WithCronSchedule(job.ScheduleCron)
                .Build();

            await scheduler.ScheduleJob(detail, trigger, ct);
            logger.LogInformation("Agendado: {Store}/{Category} cron={Cron}",
                job.Store.Slug, job.Category.Slug, job.ScheduleCron);
        }

        await scheduler.Start(ct);
        logger.LogInformation("Quartz iniciado com {Count} jobs", jobs.Count);
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}

/// <summary>Runner do job Quartz → executa a coleta do ScrapeJob.</summary>
public sealed class ScrapeJobRunner(CollectionService collection) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        var jobId = Guid.Parse(context.MergedJobDataMap.GetString("jobId")!);
        await collection.RunJobAsync(jobId, context.CancellationToken);
    }
}
