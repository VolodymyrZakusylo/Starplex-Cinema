export interface SeatMapDto {
    seatId: string;
    row: string;
    seatNumber: number;
    seatType: string;
    priceMultiplier: number;
    status: 'Available' | 'Locked' | 'Taken' | 'Inactive';
    lockedByUserId: string | null;
}

export interface CashierSaleDto {
    bookingId: string;
    orderNumber: string;
    movieTitle: string;
    sessionStartTime: string;
    totalPrice: number;
    paymentMethod: string;
    bookingTime: string;
    status: string;
    seats: string[];
}

export interface SignalRSeatsLockedDto {
    sessionId: string;
    seatIds: string[];
    lockedByUserId: string;
}

export interface SignalRSeatsReleasedDto {
    sessionId: string;
    seatIds: string[];
}

export interface BookingResponseDto {
    bookingId: string;
    totalAmount: number;
    status: string;
    clientSecret: string;
    message: string;
}