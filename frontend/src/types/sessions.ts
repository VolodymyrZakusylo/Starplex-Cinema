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
