"""CLI do spike: coleta uma categoria da Kabum e salva JSON/CSV.

Uso:
    uv run python -m openpc_spike kabum cpu --pages 3 --out /tmp/kabum_cpu.json
    uv run python -m openpc_spike kabum cpu --pages 1 --summary
"""

from __future__ import annotations

import argparse
import csv
import json
import sys
from collections import Counter

from . import fetch
from .pipeline import build_listing


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(prog="openpc-spike", description="Spike de scraper Python (Kabum).")
    parser.add_argument("store", choices=["kabum"])
    parser.add_argument("category", choices=sorted(fetch.CATEGORY_PATHS))
    parser.add_argument("--pages", type=int, default=None, help="limite de páginas (default: categoria inteira)")
    parser.add_argument("--out", help="caminho do JSON de saída")
    parser.add_argument("--csv", help="caminho do CSV de saída")
    parser.add_argument("--summary", action="store_true", help="imprime resumo em vez dos itens")
    args = parser.parse_args(argv)

    items = fetch.collect_sync(args.category, args.pages)
    listings = [build_listing(i, args.category) for i in items]

    if args.out:
        with open(args.out, "w", encoding="utf-8") as f:
            json.dump([l.to_dict() for l in listings], f, ensure_ascii=False, indent=2)

    if args.csv:
        with open(args.csv, "w", encoding="utf-8", newline="") as f:
            writer = csv.DictWriter(f, fieldnames=list(listings[0].to_dict()) if listings else ["name"])
            writer.writeheader()
            for l in listings:
                row = l.to_dict()
                row["specs"] = " | ".join(f"{k}={v}" for k, v in l.specs.items())
                writer.writerow(row)

    if args.summary or not (args.out or args.csv):
        print(f"coletados: {len(items)} itens")
        print(f"com desconto: {sum(1 for i in items if i.price_with_discount)}")
        print(f"disponíveis: {sum(1 for i in items if i.available)}")
        print(f"sem preço: {sum(1 for i in items if not i.price)}")
        specs = Counter()
        for l in listings:
            specs.update(l.specs.keys())
        if specs:
            print("specs extraídas:", dict(specs))

    return 0


if __name__ == "__main__":
    sys.exit(main())
