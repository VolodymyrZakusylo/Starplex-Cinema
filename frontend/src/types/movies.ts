import type { Session } from './sessions';

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

export type Movie = MovieDto;

export interface TmdbSearchValue {
  id: number;
  title: string;
  releaseDate?: string;
  posterPath?: string;
  overview?: string;
}
