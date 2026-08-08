import type { ProductSpec } from './api';

const brlFmt = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' });
const numFmt = new Intl.NumberFormat('pt-BR');

export const formatBRL = (v: number | null | undefined): string => (v == null ? '—' : brlFmt.format(v));

export const formatNumber = (v: number | null | undefined): string => (v == null ? '—' : numFmt.format(v));

export const formatDate = (iso: string): string =>
  new Date(iso).toLocaleDateString('pt-BR', { day: '2-digit', month: '2-digit', year: 'numeric' });

export const formatDateTime = (iso: string): string =>
  new Date(iso).toLocaleString('pt-BR', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' });

/** Rótulo pt-BR das chaves de spec (docs/specs.md §3.2). */
const SPEC_LABELS: Record<string, string> = {
  socket: 'Soquete',
  tdp_w: 'TDP (W)',
  memory_type: 'Tipo de memória',
  has_igpu: 'Vídeo integrado',
  cores: 'Núcleos',
  threads: 'Threads',
  pcie_lanes: 'Lanes PCIe',
  max_memory_speed: 'Velocidade máx. memória (MHz)',
  chipset: 'Chipset',
  form_factor: 'Formato',
  memory_slots: 'Slots de memória',
  max_memory_gb: 'Memória máx. (GB)',
  m2_slots: 'Slots M.2',
  sata_ports: 'Portas SATA',
  pcie_x16_gen: 'PCIe x16',
  bios_support: 'CPUs suportadas',
  memory_gb: 'Memória (GB)',
  length_mm: 'Comprimento (mm)',
  power_connectors: 'Conectores de energia',
  recommended_psu_w: 'Fonte recomendada (W)',
  type: 'Tipo',
  modules: 'Módulos',
  capacity_gb: 'Capacidade (GB)',
  speed_mhz: 'Velocidade (MHz)',
  height_mm: 'Altura (mm)',
  interface: 'Interface',
  wattage: 'Potência (W)',
  efficiency: 'Eficiência',
  modular: 'Modular',
  radiator_mm: 'Radiador (mm)',
  tdp_rating_w: 'TDP suportado (W)',
  supported_form_factors: 'Formatos suportados',
  max_gpu_length_mm: 'GPU máx. (mm)',
  max_cooler_height_mm: 'Cooler máx. (mm)',
  radiator_support_mm: 'Radiadores suportados',
  connectors: 'Conectores',
};

export const specLabel = (key: string): string => SPEC_LABELS[key] ?? key;

export const formatSpecValue = (key: string, spec: ProductSpec): string => {
  if (spec.valueBool != null) return spec.valueBool ? 'Sim' : 'Não';
  if (spec.valueNum != null) return formatNumber(spec.valueNum);
  return spec.valueText ?? '—';
};
