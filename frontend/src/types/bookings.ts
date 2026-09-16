export interface SeatMapDto {
  seatId: string;
  row: string;
  seatNumber: number;
  number?: number;
  seatType: string | number;
  price?: number;
  priceMultiplier?: number;
  status: 'Available' | 'Locked' | 'Sold' | 'Taken' | 'Inactive' | string;
  lockedByUserId?: string;
}

export interface CashierSaleDto {
  id: string;
  bookingCode: string;
  customerEmail: string;
  totalPrice: number;
  paymentMethod: string;
  createdAt: string;
  ticketCount: number;
}

export interface SignalRSeatsLockedDto {
  sessionId: string;
  seatIds: string[];
  userId?: string;
  lockedByUserId?: string;
}

export interface SignalRSeatsReleasedDto {
  sessionId: string;
  seatIds: string[];
}

export interface BookingResponseDto {
  bookingId: string;
  clientSecret?: string;
  totalPrice: number;
}

export interface UserTicketDto {
  id: string;
  ticketCode: string;
  movieTitle: string;
  cinemaName: string;
  hallName: string;
  startTime: string;
  row: string;
  seatNumber: number;
  price: number;
  status: string;
  qrCodeBase64: string;
}

export interface UserBookingDto {
  id: string;
  bookingCode: string;
  createdAt: string;
  totalPrice: number;
  status: string;
  tickets: UserTicketDto[];
}

export interface ScanResultDto {
  isSuccess: boolean;
  message: string;
  ticketCode?: string;
  movieTitle?: string;
  hallName?: string;
  startTime?: string;
  row?: string;
  number?: number;
}
