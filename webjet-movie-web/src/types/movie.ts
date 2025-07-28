export interface MovieSummary {
  title: string;
  year: number;
  type: string;
  posterUrl: string;
  cinemaWorldId?: string;
  filmWorldId?: string;
}

export interface MovieDetailsResult {
  title: string;
  year: number;
  type: string;
  rated: string;
  released: string;
  runtime: string;
  genre: string;
  director: string;
  writer: string;
  actors: string;
  plot: string;
  language: string;
  country: string;
  awards: string;
  posterUrl: string;
  metascore: string;
  rating: string;
  votes: string;
  cheapestPrice: number;
  cheapestProvider: string;
  cinemaWorldPrice?: number;
  filmWorldPrice?: number;
}

export interface PaginationInfo {
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

export interface GetMoviesResponse {
  movies: MovieSummary[];
  pagination: PaginationInfo;
}

export interface GetMovieDetailsResponse {
  movieDetails: MovieDetailsResult;
} 