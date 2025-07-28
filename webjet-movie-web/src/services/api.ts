import axios from 'axios';
import { GetMoviesResponse, GetMovieDetailsResponse } from '../types/movie';
import { config } from '../config/settings';

const api = axios.create({
  baseURL: config.api.baseURL,
  headers: {
    'Content-Type': 'application/json',
  },
});

export const movieApi = {
  getMovies: async (
    page: number = 1, 
    pageSize: number = 10,
    searchTerm?: string,
    sortBy?: string,
    sortDescending: boolean = false
  ): Promise<GetMoviesResponse> => {
    try {
      const params = new URLSearchParams({
        page: page.toString(),
        pageSize: pageSize.toString(),
        sortDescending: sortDescending.toString(),
      });
      
      if (searchTerm) {
        params.append('searchTerm', searchTerm);
      }
      
      if (sortBy) {
        params.append('sortBy', sortBy);
      }
      
      const response = await api.get<GetMoviesResponse>(`/movies?${params.toString()}`);
      return response.data;
    } catch (error) {
      throw error;
    }
  },

  getMovieDetails: async (title: string): Promise<GetMovieDetailsResponse> => {
    try {
      const response = await api.get<GetMovieDetailsResponse>(`/movies/details?title=${encodeURIComponent(title)}`);
      return response.data;
    } catch (error) {
      throw error;
    }
  },
}; 