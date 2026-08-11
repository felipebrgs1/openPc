using OpenPc.Domain.Compatibility;
using OpenPc.Domain.Compatibility.Rules;

namespace OpenPc.Domain.Tests;

/// <summary>Regras Error da tabela §4.2 — caso positivo, negativo e de borda cada uma.</summary>
public class ErrorRulesTests
{
    // ---------- CPU_SOCKET_MISMATCH ----------

    [Fact]
    public void CpuSocketMismatch_CpuAm5PlacaAm4_Erro()
    {
        var build = TestBuilds.Build(
            TestBuilds.Cpu(("socket", "am5")),
            TestBuilds.Mobo(("socket", "am4")));

        var result = new CpuSocketMismatchRule().Evaluate(build);

        Assert.NotNull(result);
        Assert.Equal("CPU_SOCKET_MISMATCH", result!.Code);
        Assert.Equal(Severity.Error, result.Severity);
        Assert.Equal(2, result.InvolvedProductIds.Count);
    }

    [Fact]
    public void CpuSocketMismatch_SocketsIguais_SemErro()
    {
        var build = TestBuilds.Build(
            TestBuilds.Cpu(("socket", "am5")),
            TestBuilds.Mobo(("socket", "am5")));

        Assert.Null(new CpuSocketMismatchRule().Evaluate(build));
    }

    [Fact]
    public void CpuSocketMismatch_SocketDesconhecido_NaoAvalia()
    {
        var build = TestBuilds.Build(
            TestBuilds.Cpu(("socket", "am5")),
            TestBuilds.Mobo()); // sem socket

        Assert.Null(new CpuSocketMismatchRule().Evaluate(build));
    }

    // ---------- CPU_CHIPSET_UNSUPPORTED ----------

    [Fact]
    public void CpuChipsetUnsupported_Ryzen9000EmB550_Erro()
    {
        var build = TestBuilds.Build(
            TestBuilds.Cpu("amd 9600x", ("socket", "am4")),
            TestBuilds.Mobo(("socket", "am4"), ("chipset", "b550"))); // b550: só zen2/zen3

        var result = new CpuChipsetUnsupportedRule(TestBuilds.Am5Am4Seed()).Evaluate(build);

        Assert.NotNull(result);
        Assert.Equal("CPU_CHIPSET_UNSUPPORTED", result!.Code);
    }

    [Fact]
    public void CpuChipsetUnsupported_Ryzen7000EmB650_SemErro()
    {
        var build = TestBuilds.Build(
            TestBuilds.Cpu("amd 7600", ("socket", "am5")),
            TestBuilds.Mobo(("socket", "am5"), ("chipset", "b650")));

        Assert.Null(new CpuChipsetUnsupportedRule(TestBuilds.Am5Am4Seed()).Evaluate(build));
    }

    [Fact]
    public void CpuChipsetUnsupported_SocketDiverge_DelegaParaRegraDeSocket()
    {
        var build = TestBuilds.Build(
            TestBuilds.Cpu("amd 7600", ("socket", "am5")),
            TestBuilds.Mobo(("socket", "am4"), ("chipset", "b550")));

        Assert.Null(new CpuChipsetUnsupportedRule(TestBuilds.Am5Am4Seed()).Evaluate(build));
    }

    [Fact]
    public void CpuChipsetUnsupported_ChipsetForaDaMatriz_NaoAvalia()
    {
        var build = TestBuilds.Build(
            TestBuilds.Cpu("amd 7600", ("socket", "am5")),
            TestBuilds.Mobo(("socket", "am5"), ("chipset", "h510"))); // LGA1200, fora da matriz

        Assert.Null(new CpuChipsetUnsupportedRule(TestBuilds.Am5Am4Seed()).Evaluate(build));
    }

    // ---------- RAM_TYPE_MISMATCH ----------

    [Fact]
    public void RamTypeMismatch_Ddr4EmPlacaDdr5_Erro()
    {
        var build = TestBuilds.Build(
            TestBuilds.Memory(("type", "ddr4")),
            TestBuilds.Mobo(("memory_type", "ddr5")));

        var result = new RamTypeMismatchRule().Evaluate(build);

        Assert.NotNull(result);
        Assert.Equal("RAM_TYPE_MISMATCH", result!.Code);
    }

    [Fact]
    public void RamTypeMismatch_Ddr5EmPlacaDdr5_SemErro()
    {
        var build = TestBuilds.Build(
            TestBuilds.Memory(("type", "ddr5")),
            TestBuilds.Mobo(("memory_type", "ddr5")));

        Assert.Null(new RamTypeMismatchRule().Evaluate(build));
    }

    [Fact]
    public void RamTypeMismatch_PlacaAceitaAmbos_SemErro()
    {
        var build = TestBuilds.Build(
            TestBuilds.Memory(("type", "ddr4")),
            TestBuilds.Mobo(("memory_type", "ambos")));

        Assert.Null(new RamTypeMismatchRule().Evaluate(build));
    }

    [Fact]
    public void RamTypeMismatch_UmPenteDdr5OutroDdr4_Erro()
    {
        var build = TestBuilds.Build(
            TestBuilds.Memory(("type", "ddr5")),
            TestBuilds.Memory(("type", "ddr4")),
            TestBuilds.Mobo(("memory_type", "ddr5")));

        var result = new RamTypeMismatchRule().Evaluate(build);

        Assert.NotNull(result);
        Assert.Equal("RAM_TYPE_MISMATCH", result!.Code);
        Assert.Equal(2, result.InvolvedProductIds.Count);
    }

    // ---------- RAM_CAPACITY_EXCEEDED ----------

    [Fact]
    public void RamCapacityExceeded_192GbEmPlaca128Gb_Erro()
    {
        var build = TestBuilds.Build(
            TestBuilds.Memory(("capacity_gb", 192)),
            TestBuilds.Mobo(("max_memory_gb", 128)));

        var result = new RamCapacityExceededRule().Evaluate(build);

        Assert.NotNull(result);
        Assert.Equal("RAM_CAPACITY_EXCEEDED", result!.Code);
    }

    [Fact]
    public void RamCapacityExceeded_128GbIgualMaximo_SemErro()
    {
        var build = TestBuilds.Build(
            TestBuilds.Memory(("capacity_gb", 128)),
            TestBuilds.Mobo(("max_memory_gb", 128)));

        Assert.Null(new RamCapacityExceededRule().Evaluate(build));
    }

    [Fact]
    public void RamCapacityExceeded_DoisPentesSomamCapacidade_Erro()
    {
        var build = TestBuilds.Build(
            TestBuilds.Memory(("capacity_gb", 64)),
            TestBuilds.Memory(("capacity_gb", 64)),
            TestBuilds.Mobo(("max_memory_gb", 128)));

        Assert.Null(new RamCapacityExceededRule().Evaluate(build));

        var over = TestBuilds.Build(
            TestBuilds.Memory(("capacity_gb", 64)),
            TestBuilds.Memory(("capacity_gb", 64)),
            TestBuilds.Memory(("capacity_gb", 32)),
            TestBuilds.Mobo(("max_memory_gb", 128)));

        var result = new RamCapacityExceededRule().Evaluate(over);

        Assert.NotNull(result);
        Assert.Equal("RAM_CAPACITY_EXCEEDED", result!.Code);
        Assert.Equal(4, result.InvolvedProductIds.Count);
    }

    // ---------- RAM_SLOT_OVERFLOW ----------

    [Fact]
    public void RamSlotOverflow_Kit4ModulosEm2Slots_Erro()
    {
        var build = TestBuilds.Build(
            TestBuilds.Memory(("modules", 4)),
            TestBuilds.Mobo(("memory_slots", 2)));

        var result = new RamSlotOverflowRule().Evaluate(build);

        Assert.NotNull(result);
        Assert.Equal("RAM_SLOT_OVERFLOW", result!.Code);
    }

    [Fact]
    public void RamSlotOverflow_Kit2ModulosEm2Slots_SemErro()
    {
        var build = TestBuilds.Build(
            TestBuilds.Memory(("modules", 2)),
            TestBuilds.Mobo(("memory_slots", 2)));

        Assert.Null(new RamSlotOverflowRule().Evaluate(build));
    }

    [Fact]
    public void RamSlotOverflow_DoisKitsSomamModulos()
    {
        var build = TestBuilds.Build(
            TestBuilds.Memory(("modules", 2)),
            TestBuilds.Memory(("modules", 2)),
            TestBuilds.Mobo(("memory_slots", 2)));

        Assert.NotNull(new RamSlotOverflowRule().Evaluate(build));
    }

    // ---------- MOBO_CASE_FORM_FACTOR ----------

    [Fact]
    public void MoboCaseFormFactor_MatxEmGabineteSoloAtx_Erro()
    {
        var build = TestBuilds.Build(
            TestBuilds.Mobo(("form_factor", "matx")),
            TestBuilds.Chassis(("supported_form_factors", new[] { "atx" })));

        var result = new MoboCaseFormFactorRule().Evaluate(build);

        Assert.NotNull(result);
        Assert.Equal("MOBO_CASE_FORM_FACTOR", result!.Code);
    }

    [Fact]
    public void MoboCaseFormFactor_AtxSuportado_SemErro()
    {
        var build = TestBuilds.Build(
            TestBuilds.Mobo(("form_factor", "atx")),
            TestBuilds.Chassis(("supported_form_factors", new[] { "atx", "matx" })));

        Assert.Null(new MoboCaseFormFactorRule().Evaluate(build));
    }

    [Fact]
    public void MoboCaseFormFactor_GabineteSemLista_NaoAvalia()
    {
        var build = TestBuilds.Build(
            TestBuilds.Mobo(("form_factor", "atx")),
            TestBuilds.Chassis());

        Assert.Null(new MoboCaseFormFactorRule().Evaluate(build));
    }

    // ---------- GPU_CASE_LENGTH ----------

    [Fact]
    public void GpuCaseLength_336mmEmGabinete300mm_Erro()
    {
        var build = TestBuilds.Build(
            TestBuilds.Gpu(("length_mm", 336)),
            TestBuilds.Chassis(("max_gpu_length_mm", 300)));

        var result = new GpuCaseLengthRule().Evaluate(build);

        Assert.NotNull(result);
        Assert.Equal("GPU_CASE_LENGTH", result!.Code);
    }

    [Fact]
    public void GpuCaseLength_ComprimentoIgualAoLimite_Cabe()
    {
        var build = TestBuilds.Build(
            TestBuilds.Gpu(("length_mm", 300)),
            TestBuilds.Chassis(("max_gpu_length_mm", 300)));

        Assert.Null(new GpuCaseLengthRule().Evaluate(build));
    }

    [Fact]
    public void GpuCaseLength_GpuMenorQueLimite_SemErro()
    {
        var build = TestBuilds.Build(
            TestBuilds.Gpu(("length_mm", 250)),
            TestBuilds.Chassis(("max_gpu_length_mm", 300)));

        Assert.Null(new GpuCaseLengthRule().Evaluate(build));
    }

    // ---------- COOLER_SOCKET_MISMATCH ----------

    [Fact]
    public void CoolerSocketMismatch_CoolerSoloAm4EmCpuAm5_Erro()
    {
        var build = TestBuilds.Build(
            TestBuilds.Cpu(("socket", "am5")),
            TestBuilds.Cooler(("socket_support", new[] { "am4" })));

        var result = new CoolerSocketMismatchRule().Evaluate(build);

        Assert.NotNull(result);
        Assert.Equal("COOLER_SOCKET_MISMATCH", result!.Code);
    }

    [Fact]
    public void CoolerSocketMismatch_SuporteMultiSoquete_SemErro()
    {
        var build = TestBuilds.Build(
            TestBuilds.Cpu(("socket", "am5")),
            TestBuilds.Cooler(("socket_support", new[] { "am4", "am5", "lga1700" })));

        Assert.Null(new CoolerSocketMismatchRule().Evaluate(build));
    }

    [Fact]
    public void CoolerSocketMismatch_CoolerSemLista_NaoAvalia()
    {
        var build = TestBuilds.Build(
            TestBuilds.Cpu(("socket", "am5")),
            TestBuilds.Cooler());

        Assert.Null(new CoolerSocketMismatchRule().Evaluate(build));
    }

    // ---------- COOLER_CASE_HEIGHT ----------

    [Fact]
    public void CoolerCaseHeight_170mmEmGabinete160mm_Erro()
    {
        var build = TestBuilds.Build(
            TestBuilds.Cooler(("type", "air"), ("height_mm", 170)),
            TestBuilds.Chassis(("max_cooler_height_mm", 160)));

        var result = new CoolerCaseHeightRule().Evaluate(build);

        Assert.NotNull(result);
        Assert.Equal("COOLER_CASE_HEIGHT", result!.Code);
    }

    [Fact]
    public void CoolerCaseHeight_AlturaIgualAoLimite_Cabe()
    {
        var build = TestBuilds.Build(
            TestBuilds.Cooler(("type", "air"), ("height_mm", 160)),
            TestBuilds.Chassis(("max_cooler_height_mm", 160)));

        Assert.Null(new CoolerCaseHeightRule().Evaluate(build));
    }

    [Fact]
    public void CoolerCaseHeight_AioNaoUsaAlturaDeAirCooler()
    {
        var build = TestBuilds.Build(
            TestBuilds.Cooler(("type", "aio"), ("height_mm", 170)),
            TestBuilds.Chassis(("max_cooler_height_mm", 160)));

        Assert.Null(new CoolerCaseHeightRule().Evaluate(build));
    }

    // ---------- AIO_RADIATOR_FIT ----------

    [Fact]
    public void AioRadiatorFit_Radiador360SemSuporte_Erro()
    {
        var build = TestBuilds.Build(
            TestBuilds.Cooler(("radiator_mm", 360)),
            TestBuilds.Chassis(("radiator_support_mm", new[] { 240, 280 })));

        var result = new AioRadiatorFitRule().Evaluate(build);

        Assert.NotNull(result);
        Assert.Equal("AIO_RADIATOR_FIT", result!.Code);
    }

    [Fact]
    public void AioRadiatorFit_Radiador240Suportado_SemErro()
    {
        var build = TestBuilds.Build(
            TestBuilds.Cooler(("radiator_mm", 240)),
            TestBuilds.Chassis(("radiator_support_mm", new[] { 240, 280 })));

        Assert.Null(new AioRadiatorFitRule().Evaluate(build));
    }

    [Fact]
    public void AioRadiatorFit_AirCoolerSemRadiador_NaoAvalia()
    {
        var build = TestBuilds.Build(
            TestBuilds.Cooler(("type", "air"), ("height_mm", 150)),
            TestBuilds.Chassis(("radiator_support_mm", new[] { 240 })));

        Assert.Null(new AioRadiatorFitRule().Evaluate(build));
    }

    // ---------- STORAGE_M2_OVERFLOW ----------

    [Fact]
    public void StorageM2Overflow_DoisNvmeEm1Slot_Erro()
    {
        var build = TestBuilds.Build(
            TestBuilds.Storage(("interface", "nvme")),
            TestBuilds.Storage(("interface", "nvme")),
            TestBuilds.Mobo(("m2_slots", 1)));

        var result = new StorageM2OverflowRule().Evaluate(build);

        Assert.NotNull(result);
        Assert.Equal("STORAGE_M2_OVERFLOW", result!.Code);
    }

    [Fact]
    public void StorageM2Overflow_UmNvmeEm2Slots_SemErro()
    {
        var build = TestBuilds.Build(
            TestBuilds.Storage(("interface", "nvme")),
            TestBuilds.Mobo(("m2_slots", 2)));

        Assert.Null(new StorageM2OverflowRule().Evaluate(build));
    }

    [Fact]
    public void StorageM2Overflow_SataNaoContaParaSlotsM2()
    {
        var build = TestBuilds.Build(
            TestBuilds.Storage(("interface", "sata")),
            TestBuilds.Storage(("interface", "sata")),
            TestBuilds.Mobo(("m2_slots", 1)));

        Assert.Null(new StorageM2OverflowRule().Evaluate(build));
    }

    // ---------- PSU_CONNECTOR_MISSING ----------

    [Fact]
    public void PsuConnectorMissing_Gpu2x8pinFonteSolo16pin_Erro()
    {
        var build = TestBuilds.Build(
            TestBuilds.Gpu(("power_connectors", "2x8 pinos")),
            TestBuilds.Psu(("connectors", "1x16 pinos")));

        var result = new PsuConnectorMissingRule().Evaluate(build);

        Assert.NotNull(result);
        Assert.Equal("PSU_CONNECTOR_MISSING", result!.Code);
    }

    [Fact]
    public void PsuConnectorMissing_Gpu16pinFonte16pin_SemErro()
    {
        var build = TestBuilds.Build(
            TestBuilds.Gpu(("power_connectors", "1x16 pinos")),
            TestBuilds.Psu(("connectors", "1x12vhpwr")));

        Assert.Null(new PsuConnectorMissingRule().Evaluate(build));
    }

    [Fact]
    public void PsuConnectorMissing_Gpu2x8pinFonte3x8pin_SemErro()
    {
        var build = TestBuilds.Build(
            TestBuilds.Gpu(("power_connectors", "2x8 pinos")),
            TestBuilds.Psu(("connectors", "3 x 8 pinos")));

        Assert.Null(new PsuConnectorMissingRule().Evaluate(build));
    }

    [Fact]
    public void PsuConnectorMissing_ConectorDesconhecido_NaoAvalia()
    {
        var build = TestBuilds.Build(
            TestBuilds.Gpu(("power_connectors", "1x16 pinos")),
            TestBuilds.Psu()); // sem connectors

        Assert.Null(new PsuConnectorMissingRule().Evaluate(build));
    }
}
