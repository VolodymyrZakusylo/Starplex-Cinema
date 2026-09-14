export interface DiscountDto {
  id: string;
  code: string;
  name: string;
  percentage: number;
  validFrom: string;
  validTo: string;
  isActive: boolean;
  usageLimit: number;
  usageCount: number;
}
