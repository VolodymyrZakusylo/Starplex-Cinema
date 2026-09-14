export interface AdminSeatDto {
  id: string;
  row: string;
  number: number;
  type: number;
  status: number;
}

export interface HallDto {
  id: string;
  cinemaId: string;
  name: string;
  totalRows: number;
  seatsPerRow: number;
  totalCapacity: number;
  isActive: boolean;
  seats?: AdminSeatDto[];
}

export const SeatTypeMap: { [key: number]: 'Standard' | 'VIP' | 'Disabled' } = {
  0: 'Standard',
  1: 'VIP',
  2: 'Disabled'
};

export const SeatStatusMap: { [key: number]: 'Active' | 'Inactive' } = {
  0: 'Active',
  1: 'Inactive'
};

export const SeatTypeReverseMap = { 'Standard': 0, 'VIP': 1, 'Disabled': 2 } as const;
export const SeatStatusReverseMap = { 'Active': 0, 'Inactive': 1 } as const;
