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

export interface CinemaDto {
  id: string;
  name: string;
  city: string;
  address: string;
}

export interface SessionDto {
    id: string;
    movieId: string;
    movieTitle: string;
    movieDurationInMinutes: number;
    hallId: string;
    hallName: string;
    startTime: string;
    endTime: string;
    basePrice: number;
    status: 'Active' | 'Cancelled' | number;
}

export interface Session {
  id: string;
  movieId: string;
  movieTitle: string;
  moviePosterUrl?: string;
  movieDurationInMinutes?: number;
  hallId: string;
  cinemaId: string;
  hallName: string;
  startTime: string;
  endTime: string;
  basePrice: number;
  status: number | string;
}

export interface MovieShortDto {
    id: string;
    title: string;
    genre: string;
    durationInMinutes: number;
    status: number;
}

export interface MovieDto {
    id: string;
    tmdbId?: number;
    title: string;
    slug?: string;
    description?: string;
    durationInMinutes: number;
    posterUrl: string;
    backdropUrl?: string;
    posterStoragePath?: string;
    genre?: string;
    trailerUrl?: string;
    ageRating?: string;
    tmdbRating?: number;
    status: number;
    releaseDate?: string;
    effectivePosterUrl?: string;
    sessions?: Session[];
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
export type Movie = MovieDto;