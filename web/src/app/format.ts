import type { ProductSpec } from './api';

const brlFmt = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' });
const numFmt = new Intl.NumberFormat('pt-BR');

export const formatBRL = (v: number | null | undefined): string => (v == null ? '—' : brlFmt.format(v));

export const formatNumber = (v: number | null | undefined): string => (v == null ? '—' : numFmt.format(v));

export const formatDate = (iso: string): string =>
  new Date(iso).toLocaleDateString('pt-BR', { day: '2-digit', month: '2-digit', year: 'numeric' });

export const formatDateTime = (iso: string): string =>
  new Date(iso).toLocaleString('pt-BR', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' });

/** Rótulo pt-BR das chaves de spec (docs/specs.md §3.2 + chaves de detalhe). */
const SPEC_LABELS: Record<string, string> = {
  // cpu
  socket: 'Soquete',
  cores: 'Núcleos',
  threads: 'Threads',
  base_clock_mhz: 'Clock base',
  boost_clock_mhz: 'Clock boost',
  cache_l2_mb: 'Cache L2',
  cache_l3_mb: 'Cache L3',
  tdp_w: 'TDP',
  has_igpu: 'Vídeo integrado',
  pcie_lanes: 'Lanes PCIe',
  max_memory_speed: 'Velocidade máx. memória',
  cooler_included: 'Cooler incluso',
  // gpu
  gpu_model: 'Motor gráfico',
  reference_model: 'Modelo de referência',
  gpu_chip: 'Chip',
  cuda_cores: 'Núcleos CUDA',
  stream_processors: 'Processadores de fluxo',
  compute_units: 'Unidades de computação',
  tensor_cores: 'Núcleos Tensor',
  rt_cores: 'Núcleos RT',
  xe_cores: 'Núcleos Xe',
  game_clock_mhz: 'Game clock',
  memory_gb: 'Memória',
  memory_type: 'Tipo de memória',
  memory_bus_bits: 'Interface de memória',
  memory_clock_gbps: 'Relógio de memória',
  bandwidth_gbps: 'Largura de banda',
  directx: 'DirectX',
  opengl: 'OpenGL',
  vulkan: 'Vulkan',
  video_outputs: 'Saídas de vídeo',
  max_resolution: 'Resolução máx.',
  multi_monitor: 'Multivisualização',
  hdcp: 'HDCP',
  process_nm: 'Litografia',
  transistors: 'Transistores',
  launch: 'Lançamento',
  interface: 'Interface',
  series: 'Série',
  // gpu / case / cooler
  length_mm: 'Comprimento',
  width_mm: 'Largura',
  height_mm: 'Altura',
  slots: 'Slots',
  weight: 'Peso',
  power_connectors: 'Conectores de energia',
  recommended_psu_w: 'Fonte recomendada',
  // memory
  type: 'Tipo',
  modules: 'Módulos',
  capacity_gb: 'Capacidade',
  speed_mhz: 'Velocidade',
  cas_latency: 'Latência (CAS)',
  voltage_v: 'Tensão',
  heatsink: 'Dissipador',
  rgb: 'RGB',
  // storage
  form_factor: 'Formato',
  read_mbps: 'Leitura',
  write_mbps: 'Gravação',
  tbw: 'TBW',
  nand: 'NAND',
  dram_cache: 'Cache DRAM',
  // psu
  wattage: 'Potência',
  efficiency: 'Eficiência',
  modular: 'Modular',
  connectors: 'Conectores',
  fan_mm: 'Ventoinha',
  // motherboard
  chipset: 'Chipset',
  memory_slots: 'Slots de memória',
  max_memory_gb: 'Memória máx.',
  m2_slots: 'Slots M.2',
  sata_ports: 'Portas SATA',
  pcie_x16_gen: 'PCIe x16',
  bios_support: 'CPUs suportadas',
  usb_ports: 'USB',
  network: 'Rede',
  wifi: 'Wi-Fi',
  audio: 'Áudio',
  // cooler
  socket_support: 'Soquetes suportados',
  radiator_mm: 'Radiador',
  fans: 'Fans',
  max_rpm: 'RPM máx.',
  tdp_rating_w: 'TDP suportado',
  noise_dba: 'Ruído',
  // case
  supported_form_factors: 'Formatos suportados',
  max_gpu_length_mm: 'GPU máx.',
  max_cooler_height_mm: 'Cooler máx.',
  radiator_support_mm: 'Radiadores suportados',
  psu_form_factor: 'Fonte suportada',
  included_fans: 'Fans incluídas',
};

export const specLabel = (key: string): string => SPEC_LABELS[key] ?? key;

/** Unidade de exibição por chave (valores numéricos). */
const SPEC_UNITS: Record<string, string> = {
  base_clock_mhz: 'MHz',
  boost_clock_mhz: 'MHz',
  game_clock_mhz: 'MHz',
  max_memory_speed: 'MHz',
  speed_mhz: 'MHz',
  memory_gb: 'GB',
  capacity_gb: 'GB',
  max_memory_gb: 'GB',
  cache_l2_mb: 'MB',
  cache_l3_mb: 'MB',
  memory_clock_gbps: 'Gbps',
  bandwidth_gbps: 'GB/s',
  memory_bus_bits: 'bits',
  tdp_w: 'W',
  recommended_psu_w: 'W',
  wattage: 'W',
  tdp_rating_w: 'W',
  length_mm: 'mm',
  width_mm: 'mm',
  height_mm: 'mm',
  radiator_mm: 'mm',
  fan_mm: 'mm',
  max_gpu_length_mm: 'mm',
  max_cooler_height_mm: 'mm',
  process_nm: 'nm',
  voltage_v: 'V',
  noise_dba: 'dBA',
  read_mbps: 'MB/s',
  write_mbps: 'MB/s',
};

export const formatSpecValue = (key: string, spec: ProductSpec): string => {
  if (spec.valueBool != null) return spec.valueBool ? 'Sim' : 'Não';
  const unit = SPEC_UNITS[key];
  if (spec.valueNum != null) return unit ? `${numFmt.format(spec.valueNum)} ${unit}` : numFmt.format(spec.valueNum);
  const text = spec.valueText ?? '—';
  if (unit && /^\d+([.,]\d+)?$/.test(text)) {
    return `${numFmt.format(Number(text.replace(',', '.')))} ${unit}`;
  }
  return text;
};

export interface SpecGroup {
  title: string;
  items: ProductSpec[];
}

/** Seções de specs por categoria (ordem de exibição; sobras vão para "Outras"). */
const SPEC_GROUPS: Record<string, { title: string; keys: string[] }[]> = {
  gpu: [
    {
      title: 'Chip gráfico',
      keys: ['gpu_model', 'gpu_chip', 'process_nm', 'transistors', 'interface', 'launch', 'reference_model'],
    },
    {
      title: 'Performance',
      keys: [
        'cuda_cores', 'stream_processors', 'compute_units', 'tensor_cores', 'rt_cores', 'xe_cores',
        'base_clock_mhz', 'boost_clock_mhz', 'game_clock_mhz', 'directx', 'opengl', 'vulkan',
      ],
    },
    {
      title: 'Memória',
      keys: ['memory_gb', 'memory_type', 'memory_bus_bits', 'memory_clock_gbps', 'bandwidth_gbps'],
    },
    { title: 'Alimentação', keys: ['tdp_w', 'power_connectors', 'recommended_psu_w'] },
    { title: 'Dimensões', keys: ['length_mm', 'width_mm', 'height_mm', 'slots', 'weight'] },
    { title: 'Saídas de vídeo', keys: ['video_outputs', 'max_resolution', 'multi_monitor', 'hdcp'] },
    { title: 'Catálogo', keys: ['series'] },
  ],
  cpu: [
    {
      title: 'Processador',
      keys: ['reference_model', 'cores', 'threads', 'base_clock_mhz', 'boost_clock_mhz', 'process_nm', 'launch'],
    },
    { title: 'Cache', keys: ['cache_l2_mb', 'cache_l3_mb'] },
    {
      title: 'Plataforma',
      keys: ['socket', 'tdp_w', 'has_igpu', 'pcie_lanes', 'memory_type', 'max_memory_speed', 'cooler_included'],
    },
  ],
  memory: [
    { title: 'Memória', keys: ['type', 'capacity_gb', 'modules', 'speed_mhz', 'cas_latency', 'voltage_v', 'heatsink', 'height_mm', 'rgb'] },
  ],
  storage: [
    { title: 'Armazenamento', keys: ['capacity_gb', 'interface', 'form_factor', 'read_mbps', 'write_mbps', 'tbw', 'dram_cache', 'nand'] },
  ],
  psu: [
    { title: 'Fonte', keys: ['wattage', 'efficiency', 'modular', 'fan_mm', 'connectors', 'length_mm', 'width_mm', 'height_mm'] },
  ],
  motherboard: [
    { title: 'Chipset', keys: ['socket', 'chipset', 'form_factor', 'bios_support'] },
    { title: 'Memória', keys: ['memory_type', 'memory_slots', 'max_memory_gb', 'max_memory_speed'] },
    { title: 'Expansão', keys: ['m2_slots', 'sata_ports', 'pcie_x16_gen'] },
    { title: 'Conectividade', keys: ['usb_ports', 'network', 'wifi', 'audio'] },
    { title: 'Dimensões', keys: ['length_mm', 'width_mm'] },
  ],
  cooler: [
    { title: 'Cooler', keys: ['type', 'socket_support', 'height_mm', 'radiator_mm', 'fans', 'max_rpm', 'tdp_rating_w', 'noise_dba', 'rgb'] },
  ],
  case: [
    {
      title: 'Gabinete',
      keys: [
        'supported_form_factors', 'max_gpu_length_mm', 'max_cooler_height_mm', 'radiator_support_mm',
        'psu_form_factor', 'included_fans', 'length_mm', 'width_mm', 'height_mm', 'weight',
      ],
    },
  ],
};

/** Agrupa as specs do produto nas seções da categoria (sobras em "Outras especificações"). */
export const specGroups = (category: string, specs: ProductSpec[]): SpecGroup[] => {
  const groups = SPEC_GROUPS[category] ?? [];
  const byKey = new Map(specs.map((s) => [s.key, s]));
  const used = new Set<string>();
  const result: SpecGroup[] = [];

  for (const group of groups) {
    const items = group.keys
      .map((key) => byKey.get(key))
      .filter((s): s is ProductSpec => s != null)
      .map((s) => (used.add(s.key), s));
    if (items.length) result.push({ title: group.title, items });
  }

  const leftovers = specs.filter((s) => !used.has(s.key));
  if (leftovers.length) result.push({ title: 'Outras especificações', items: leftovers });

  return result;
};
