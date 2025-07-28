import React, { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { movieApi } from '../services/api';
import { MovieDetailsResult } from '../types/movie';
import LoadingSpinner from '../components/LoadingSpinner';

// Skeleton for details page
const SkeletonMovieDetails: React.FC = () => (
  <div className="bg-white rounded-lg shadow-lg overflow-hidden animate-pulse">
    <div className="flex flex-col md:flex-row">
      <div className="md:w-1/3 bg-gray-200 aspect-[2/3]" />
      <div className="md:w-2/3 p-6">
        <div className="h-8 bg-gray-200 rounded w-2/3 mb-4" />
        <div className="h-6 bg-gray-100 rounded w-1/3 mb-6" />
        <div className="mb-6 p-4 bg-blue-50 rounded-lg">
          <div className="h-4 bg-blue-100 rounded w-1/2 mb-2" />
          <div className="h-4 bg-blue-100 rounded w-1/3 mb-2" />
          <div className="h-4 bg-blue-100 rounded w-1/4" />
        </div>
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4 mb-6">
          <div className="h-4 bg-gray-100 rounded w-2/3 mb-2" />
          <div className="h-4 bg-gray-100 rounded w-1/2 mb-2" />
          <div className="h-4 bg-gray-100 rounded w-1/3 mb-2" />
          <div className="h-4 bg-gray-100 rounded w-1/4 mb-2" />
          <div className="h-4 bg-gray-100 rounded w-1/2 mb-2" />
          <div className="h-4 bg-gray-100 rounded w-1/3 mb-2" />
        </div>
        <div className="mb-6">
          <div className="h-4 bg-gray-100 rounded w-1/2 mb-2" />
          <div className="h-4 bg-gray-100 rounded w-1/3 mb-2" />
          <div className="h-4 bg-gray-100 rounded w-1/4 mb-2" />
        </div>
        <div className="mb-6">
          <div className="h-4 bg-gray-100 rounded w-1/2 mb-2" />
          <div className="h-4 bg-gray-100 rounded w-full mb-2" />
        </div>
        <div className="bg-gray-50 p-6 rounded-lg">
          <div className="h-6 bg-gray-200 rounded w-1/4 mb-4" />
          <div className="h-8 bg-green-100 rounded w-1/3 mb-2" />
          <div className="h-4 bg-gray-100 rounded w-1/2" />
        </div>
      </div>
    </div>
  </div>
);

const MovieDetailsPage: React.FC = () => {
  const { title } = useParams<{ title: string }>();
  const navigate = useNavigate();
  const [details, setDetails] = useState<MovieDetailsResult | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchMovieDetails = async () => {
      if (!title) return;
      try {
        setLoading(true);
        setError(null);
        const response = await movieApi.getMovieDetails(decodeURIComponent(title));
        setDetails(response.movieDetails);
      } catch (err: any) {
        const errorMessage = err.response?.data?.error || 
                           err.message || 
                           'Could not load movie details. Please try again later.';
        setError(errorMessage);
      } finally {
        setLoading(false);
      }
    };
    fetchMovieDetails();
  }, [title]);

  const placeholderImage = 'https://via.placeholder.com/300x450/cccccc/666666?text=No+Image';
  const handleImageError = (e: React.SyntheticEvent<HTMLImageElement, Event>) => {
    e.currentTarget.src = placeholderImage;
  };

  return (
    <div className="min-h-screen bg-gray-50">
      <div className="container mx-auto px-4 py-8">
        <button 
          onClick={() => navigate(-1)} 
          className="text-blue-500 hover:text-blue-700 mb-6 flex items-center"
        >
          <svg className="w-5 h-5 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 19l-7-7 7-7" />
          </svg>
          Back to Movies
        </button>
        {loading ? (
          <SkeletonMovieDetails />
        ) : error ? (
          <div className="text-center my-12">
            <div className="text-red-500 text-xl mb-4">{error}</div>
            <button 
              onClick={() => navigate(-1)} 
              className="bg-blue-500 hover:bg-blue-700 text-white font-bold py-2 px-4 rounded mr-2"
            >
              Go Back
            </button>
            <button 
              onClick={() => window.location.reload()} 
              className="bg-gray-500 hover:bg-gray-700 text-white font-bold py-2 px-4 rounded"
            >
              Try Again
            </button>
          </div>
        ) : !details ? (
          <div className="text-center my-12">
            <div className="text-gray-500 text-xl mb-4">Movie not found</div>
            <button 
              onClick={() => navigate(-1)} 
              className="bg-blue-500 hover:bg-blue-700 text-white font-bold py-2 px-4 rounded"
            >
              Go Back
            </button>
          </div>
        ) : (
          <div className="bg-white rounded-lg shadow-lg overflow-hidden">
            <div className="flex flex-col md:flex-row">
              {/* Poster */}
              <div className="md:w-1/3">
                <img 
                  src={details.posterUrl || placeholderImage} 
                  alt={details.title} 
                  className="w-full h-auto object-cover" 
                  onError={handleImageError}
                />
              </div>
              {/* Details */}
              <div className="md:w-2/3 p-6">
                <div className="mb-6">
                  <h1 className="text-3xl font-bold text-gray-900 mb-2">
                    {details.title}
                  </h1>
                  <p className="text-xl text-gray-600 mb-4">
                    ({details.year}) • {details.type}
                  </p>
                  {/* Ratings Section */}
                  <div className="mb-6 p-4 bg-blue-50 rounded-lg">
                    <h3 className="font-semibold text-gray-700 mb-3">Ratings & Awards</h3>
                    <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                      <div>
                        <span className="font-semibold text-gray-700">Rating:</span> {details.rating}/10
                      </div>
                      <div>
                        <span className="font-semibold text-gray-700">Metascore:</span> {details.metascore}
                      </div>
                      <div>
                        <span className="font-semibold text-gray-700">Votes:</span> {details.votes}
                      </div>
                    </div>
                    {details.awards && (
                      <div className="mt-3">
                        <span className="font-semibold text-gray-700">Awards:</span>
                        <p className="mt-1 text-gray-600 text-sm">{details.awards}</p>
                      </div>
                    )}
                  </div>
                  {/* Basic Info */}
                  <div className="grid grid-cols-1 md:grid-cols-2 gap-4 mb-6">
                    <div>
                      <span className="font-semibold text-gray-700">Rated:</span> {details.rated}
                    </div>
                    <div>
                      <span className="font-semibold text-gray-700">Released:</span> {details.released}
                    </div>
                    <div>
                      <span className="font-semibold text-gray-700">Runtime:</span> {details.runtime}
                    </div>
                    <div>
                      <span className="font-semibold text-gray-700">Genre:</span> {details.genre}
                    </div>
                    <div>
                      <span className="font-semibold text-gray-700">Language:</span> {details.language}
                    </div>
                    <div>
                      <span className="font-semibold text-gray-700">Country:</span> {details.country}
                    </div>
                  </div>
                  {/* Cast & Crew */}
                  <div className="mb-6">
                    <div className="mb-4">
                      <span className="font-semibold text-gray-700">Director:</span> {details.director}
                    </div>
                    <div className="mb-4">
                      <span className="font-semibold text-gray-700">Writer:</span> {details.writer}
                    </div>
                    <div className="mb-4">
                      <span className="font-semibold text-gray-700">Actors:</span> {details.actors}
                    </div>
                  </div>
                  {/* Plot */}
                  <div className="mb-6">
                    <span className="font-semibold text-gray-700">Plot:</span>
                    <p className="mt-2 text-gray-600">{details.plot}</p>
                  </div>
                </div>
                {/* Pricing Section */}
                <div className="bg-gray-50 p-6 rounded-lg">
                  <h2 className="text-2xl font-bold text-gray-900 mb-4">Best Price</h2>
                  {details.cheapestPrice ? (
                    <div className="mb-4">
                      <div className="text-3xl font-bold text-green-600">
                        ${details.cheapestPrice.toFixed(2)}
                      </div>
                      <div className="text-gray-600">
                        via {details.cheapestProvider}
                      </div>
                    </div>
                  ) : (
                    <div className="text-gray-600">Price not available</div>
                  )}
                  {/* Provider Prices */}
                  {(details.cinemaWorldPrice || details.filmWorldPrice) && (
                    <div className="mt-4 pt-4 border-t border-gray-200">
                      <h3 className="font-semibold text-gray-700 mb-2">All Prices:</h3>
                      <div className="space-y-2">
                        {(() => {
                          const prices = [
                            { name: 'CinemaWorld', price: details.cinemaWorldPrice },
                            { name: 'FilmWorld', price: details.filmWorldPrice }
                          ].filter(p => p.price != null)
                           .sort((a, b) => (a.price || 0) - (b.price || 0));
                          return prices.map((provider, index) => (
                            <div key={provider.name} className="flex justify-between">
                              <span>{provider.name}:</span>
                              <span className={`font-semibold ${index === 0 ? 'text-green-600' : ''}`}>
                                ${provider.price!.toFixed(2)}
                              </span>
                            </div>
                          ));
                        })()}
                      </div>
                    </div>
                  )}
                </div>
              </div>
            </div>
          </div>
        )}
      </div>
    </div>
  );
};

export default MovieDetailsPage; 