import React from 'react';
import { Link } from 'react-router-dom';
import { MovieSummary } from '../types/movie';

interface MovieCardProps {
  movie: MovieSummary;
}

const MovieCard: React.FC<MovieCardProps> = ({ movie }) => {
  const placeholderImage = `data:image/svg+xml;base64,${btoa(`
    <svg width="300" height="450" xmlns="http://www.w3.org/2000/svg">
      <rect width="100%" height="100%" fill="#cccccc"/>
      <text x="50%" y="50%" font-family="Arial, sans-serif" font-size="16" fill="#666666" text-anchor="middle" dy=".3em">No Image</text>
    </svg>
  `)}`;

  const handleImageError = (e: React.SyntheticEvent<HTMLImageElement, Event>) => {
    e.currentTarget.src = placeholderImage;
  };

  return (
    <Link 
      to={`/movie/${encodeURIComponent(movie.title)}`} 
      className="block bg-white rounded-lg shadow-md hover:shadow-lg transition-shadow duration-300 overflow-hidden"
    >
      <div className="aspect-[2/3] overflow-hidden">
        <img 
          src={movie.posterUrl || placeholderImage} 
          alt={movie.title} 
          className="object-cover w-full h-full" 
          onError={handleImageError}
        />
      </div>
      <div className="p-4">
        <h2 className="font-semibold text-lg text-gray-900 mb-1 line-clamp-2">
          {movie.title}
        </h2>
        <p className="text-sm text-gray-600 mb-2">
          {movie.year} • {movie.type}
        </p>
        <div className="flex gap-1">
          {movie.cinemaWorldId && (
            <span className="inline-block bg-blue-100 text-blue-800 text-xs px-2 py-1 rounded">
              CinemaWorld
            </span>
          )}
          {movie.filmWorldId && (
            <span className="inline-block bg-green-100 text-green-800 text-xs px-2 py-1 rounded">
              FilmWorld
            </span>
          )}
        </div>
      </div>
    </Link>
  );
};

export default MovieCard; 