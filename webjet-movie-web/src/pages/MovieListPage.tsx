import React, { useEffect, useState, useCallback } from 'react';
import { movieApi } from '../services/api';
import { MovieSummary, PaginationInfo } from '../types/movie';
import MovieCard from '../components/MovieCard';
import LoadingSpinner from '../components/LoadingSpinner';
import Pagination from '../components/Pagination';
import SearchAndSort from '../components/SearchAndSort';

// Skeleton placeholder for loading state
const SkeletonMovieCard: React.FC = () => (
  <div className="bg-white rounded-lg shadow-md overflow-hidden animate-pulse">
    <div className="aspect-[2/3] bg-gray-200" />
    <div className="p-4">
      <div className="h-5 bg-gray-200 rounded w-3/4 mb-2" />
      <div className="h-4 bg-gray-100 rounded w-1/2 mb-2" />
      <div className="flex gap-1">
        <div className="h-4 w-16 bg-gray-100 rounded" />
        <div className="h-4 w-16 bg-gray-100 rounded" />
      </div>
    </div>
  </div>
);

const MovieListPage: React.FC = () => {
  const [movies, setMovies] = useState<MovieSummary[]>([]);
  const [pagination, setPagination] = useState<PaginationInfo | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [currentPage, setCurrentPage] = useState(1);
  const [pageSize] = useState(6); // Show 6 movies per page (2 rows of 3)
  const [searchTerm, setSearchTerm] = useState('');
  const [sortBy, setSortBy] = useState('');
  const [sortDescending, setSortDescending] = useState(false);

  const fetchMovies = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const response = await movieApi.getMovies(
        currentPage, 
        pageSize, 
        searchTerm || undefined,
        sortBy || undefined,
        sortDescending
      );
      setMovies(response.movies);
      setPagination(response.pagination);
    } catch (err: any) {
      const errorMessage = err.response?.data?.error || 
                         err.message || 
                         'Could not load movies. Please try again later.';
      setError(errorMessage);
    } finally {
      setLoading(false);
    }
  }, [currentPage, pageSize, searchTerm, sortBy, sortDescending]);

  useEffect(() => {
    fetchMovies();
  }, [fetchMovies]);

  const handlePageChange = (page: number) => {
    setCurrentPage(page);
    window.scrollTo({ top: 0, behavior: 'smooth' });
  };

  const handleSearchChange = useCallback((newSearchTerm: string) => {
    setSearchTerm(newSearchTerm);
    setCurrentPage(1); // Reset to first page when searching
  }, []);

  const handleSortChange = useCallback((newSortBy: string, newSortDescending: boolean) => {
    setSortBy(newSortBy);
    setSortDescending(newSortDescending);
    setCurrentPage(1); // Reset to first page when sorting
  }, []);

  return (
    <div className="min-h-screen bg-gray-50">
      <div className="container mx-auto px-4 py-8">
        <div className="mb-5">
          <h1 className="text-3xl font-bold text-gray-900 mb-2">Movie Collection</h1>
          <p className="text-gray-700">Discover movies and find the best prices</p>
        </div>
        {/* Search and Sort Controls */}
        <SearchAndSort
          searchTerm={searchTerm}
          sortBy={sortBy}
          sortDescending={sortDescending}
          onSearchChange={handleSearchChange}
          onSortChange={handleSortChange}
        />
        {error && (
          <div className="my-8 text-center">
            <div className="text-red-500 text-xl mb-4">{error}</div>
            <button 
              onClick={() => window.location.reload()} 
              className="bg-blue-500 hover:bg-blue-700 text-white font-bold py-2 px-4 rounded"
            >
              Try Again
            </button>
          </div>
        )}
        {pagination && !loading && (
          <div className="mb-3 text-gray-600">
            <p className="text-gray-400 text-sm">
              Showing {movies.length} of {pagination.totalItems} movies
              {pagination.totalPages > 1 && ` (Page ${pagination.page} of ${pagination.totalPages})`}
              {searchTerm && ` matching "${searchTerm}"`}
            </p>
          </div>
        )}
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6 mb-8">
          {loading
            ? Array.from({ length: pageSize }).map((_, i) => <SkeletonMovieCard key={i} />)
            : movies.map((movie) => (
                <MovieCard key={`${movie.title}-${movie.year}`} movie={movie} />
              ))}
        </div>
        {loading && <LoadingSpinner />}
        {pagination && !loading && (
          <Pagination pagination={pagination} onPageChange={handlePageChange} />
        )}
      </div>
    </div>
  );
};

export default MovieListPage; 