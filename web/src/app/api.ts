// Tipos espelhados das respostas da API (camelCase do ASP.NET).

export interface Category {
  id: string;
  slug: string;
  name: string;
  displayOrder: number;
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

export const categoryLabel = (slug: string): string => CATEGORY_LABELS[slug] ?? slug;
