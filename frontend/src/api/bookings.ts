import api from './axios';
import type {
  SeatMapDto,
  CashierSaleDto,
  BookingResponseDto,
  UserBookingDto,
  ScanResultDto
} from '@/types/bookings';

export const bookingsApi = {
  getSeats: async (sessionId: string): Promise<SeatMapDto[]> => {
    const response = await api.get<SeatMapDto[]>(`/Bookings/session/${sessionId}/seats`);
    return response.data;
  },

  getCashierSales: async (): Promise<CashierSaleDto[]> => {
    const response = await api.get<CashierSaleDto[]>('/Bookings/cashier-sales');
    return response.data;
  },

  lockSeat: async (sessionId: string, seatId: string): Promise<boolean> => {
    const response = await api.post<{ isSuccess: boolean }>('/Bookings/lock', {
      sessionId,
      seatIds: [seatId],
    });
    return response.data.isSuccess;
  },

  unlockSeat: async (sessionId: string, seatId: string): Promise<boolean> => {
    const response = await api.post<{ isSuccess: boolean }>('/Bookings/unlock', {
      sessionId,
      seatIds: [seatId],
    });
    return response.data.isSuccess;
  },

  create: async (sessionId: string, seatIds: string[], discountCode?: string): Promise<BookingResponseDto> => {
    const response = await api.post<BookingResponseDto>('/Bookings/create', {
      sessionId,
      seatIds,
      promoCode: discountCode || null,
    });
    return response.data;
  },

  confirm: async (bookingId: string): Promise<void> => {
    await api.post('/Bookings/confirm', { bookingId });
  },

  cashierSell: async (sessionId: string, seatIds: string[], paymentMethod: string): Promise<string> => {
    const response = await api.post<string>('/Bookings/cashier-sell', {
      sessionId,
      seatIds,
      paymentMethod,
    });
    return response.data;
  },

  getMyBookings: async (): Promise<UserBookingDto[]> => {
    const response = await api.get<UserBookingDto[]>('/Bookings/my-bookings');
    return response.data;
  },

  downloadTickets: async (bookingId: string): Promise<Blob> => {
    const response = await api.get(`/Bookings/${bookingId}/tickets/download-all`, {
      responseType: 'blob',
    });
    return response.data;
  },

  cancelTicket: async (ticketId: string): Promise<void> => {
    await api.post(`/Bookings/tickets/${ticketId}/cancel`);
  },

  scanTicket: async (ticketCode: string): Promise<ScanResultDto> => {
    const response = await api.post<ScanResultDto>('/Bookings/scan-ticket', { ticketCode });
    return response.data;
  }
};
