// Tipos espelhados das respostas da API (camelCase do ASP.NET).

export interface Category {
  id: string;
  slug: string;
  name: string;
  displayOrder: number;
}

/** Loja com a quantidade de itens com valor (preço em estoque) — banner da home. */
export interface StoreStats {
  id: string;
  slug: string;
  name: string;
  baseUrl: string;
  itemCount: number;
}

export interface BlockedBy {
  code: string;
  message: string;
}

export interface ProductListItem {
  id: string;
  name: string;
  brand: string;
  model: string;
  partNumber: string | null;
  imageUrl: string | null;
  categorySlug: string;
  price: number | null;
  storeCount: number;
  blockedBy?: BlockedBy[];
}

export interface ProductsResponse {
  items: ProductListItem[];
  total: number;
}

export interface ProductSpec {
  key: string;
  valueText: string | null;
  valueNum: number | null;
  valueBool: boolean | null;
  /** fonte do valor: reference | title | page | manual (precedência page > title > reference) */
  source?: string;
}

export interface Listing {
  storeSlug: string;
  storeName: string;
  priceCash: number | null;
  priceCard: number | null;
  installments: number | null;
  installmentText: string | null;
  inStock: boolean;
  url: string;
  thumbnail: string | null;
  lastSeenAt: string;
}

export interface ProductDetail {
  id: string;
  name: string;
  brand: string;
  model: string;
  partNumber: string | null;
  imageUrl: string | null;
  categorySlug: string;
  specs: ProductSpec[];
  listings: Listing[];
}

export interface PricePoint {
  date: string;
  price: number;
}

export interface OfferItem {
  product: ProductListItem;
  currentPrice: number;
  price24hAgo: number | null;
  price7dAgo: number | null;
  dropPercent24h: number | null;
  dropPercent7d: number | null;
  lowestInDays: number;
  isAnomaly: boolean;
}

export interface OffersResponse {
  items: OfferItem[];
  period: '24h' | '7d';
}

export interface CreateAlertRequest {
  productId: string;
  email: string;
  targetPrice: number;
}

export interface AlertResponse {
  id: string;
  productId: string;
  email: string;
  targetPrice: number;
  confirmed: boolean;
  confirmUrl: string;
}

export interface BuildItemDto {
  id: string;
  category: string;
  productId: string | null;
  name: string | null;
  brand: string | null;
  model: string | null;
  imageUrl: string | null;
  storeSlug: string | null;
  price: number | null;
}

export interface IssueDto {
  code: string;
  message: string;
  products: string[];
}

export interface CompatibilityDto {
  errors: IssueDto[];
  warnings: IssueDto[];
  infos: IssueDto[];
}

export interface WattageDto {
  baseW: number;
  recommendedW: number;
  known: boolean;
}

export interface BuildDto {
  slug: string;
  name: string;
  isPublic: boolean;
  createdAt: string;
  updatedAt: string;
  items: BuildItemDto[];
  totalPrice: number | null;
  wattage: WattageDto;
  compatibility: CompatibilityDto;
}

export interface StoreTotal {
  storeSlug: string;
  storeName: string;
  total: number;
  coveredItems: number;
  totalItems: number;
}

export interface PriceComparison {
  perStore: StoreTotal[];
  bestIndividual: {
    total: number;
    items: { category: string; productId: string; storeSlug: string | null; price: number | null }[];
  };
}

/** Nome curto de exibição de cada slot do montador. */
export const CATEGORY_LABELS: Record<string, string> = {
  cpu: 'Processador',
  motherboard: 'Placa-mãe',
  gpu: 'Placa de vídeo',
  memory: 'Memória RAM',
  storage: 'Armazenamento',
  psu: 'Fonte',
  case: 'Gabinete',
  cooler: 'Cooler',
};

/**
 * Categorias estáticas do catálogo — usadas no nav/home/montador no primeiro
 * paint (sem esperar a API) para não haver layout shift.
 */
export const NAV_CATEGORIES: Category[] = [
  { id: 'cpu', slug: 'cpu', name: 'Processador', displayOrder: 1 },
  { id: 'motherboard', slug: 'motherboard', name: 'Placa-mãe', displayOrder: 2 },
  { id: 'gpu', slug: 'gpu', name: 'Placa de vídeo', displayOrder: 3 },
  { id: 'memory', slug: 'memory', name: 'Memória RAM', displayOrder: 4 },
  { id: 'storage', slug: 'storage', name: 'Armazenamento', displayOrder: 5 },
  { id: 'psu', slug: 'psu', name: 'Fonte', displayOrder: 6 },
  { id: 'case', slug: 'case', name: 'Gabinete', displayOrder: 7 },
  { id: 'cooler', slug: 'cooler', name: 'Cooler', displayOrder: 8 },
];

/** Slots que aceitam mais de uma peça (memória e armazenamento). */
export const MULTI_SLOT_CATEGORIES = new Set(['memory', 'storage']);

export const isMultiSlot = (slug: string): boolean => MULTI_SLOT_CATEGORIES.has(slug);

export const categoryLabel = (slug: string): string => CATEGORY_LABELS[slug] ?? slug;
