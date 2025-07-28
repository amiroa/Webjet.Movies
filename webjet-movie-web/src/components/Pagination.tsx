import React from 'react';
import { PaginationInfo } from '../types/movie';

interface PaginationProps {
  pagination: PaginationInfo;
  onPageChange: (page: number) => void;
}

const Pagination: React.FC<PaginationProps> = ({ pagination, onPageChange }) => {
  const { page, totalPages, hasNextPage, hasPreviousPage } = pagination;

  const handlePageChange = (newPage: number) => {
    if (newPage >= 1 && newPage <= totalPages) {
      onPageChange(newPage);
    }
  };

  const renderPageNumbers = () => {
    const pages = [];
    const maxVisiblePages = 5;
    let startPage = Math.max(1, page - Math.floor(maxVisiblePages / 2));
    let endPage = Math.min(totalPages, startPage + maxVisiblePages - 1);

    if (endPage - startPage + 1 < maxVisiblePages) {
      startPage = Math.max(1, endPage - maxVisiblePages + 1);
    }

    for (let i = startPage; i <= endPage; i++) {
      pages.push(
        <button
          key={i}
          onClick={() => handlePageChange(i)}
          className={`px-3 py-2 mx-1 rounded ${
            i === page
              ? 'bg-blue-500 text-white'
              : 'bg-white text-gray-700 hover:bg-gray-50 border'
          }`}
        >
          {i}
        </button>
      );
    }

    return pages;
  };

  if (totalPages <= 1) {
    return null;
  }

  return (
    <div className="flex items-center justify-center space-x-2 mt-8">
      <button
        onClick={() => handlePageChange(page - 1)}
        disabled={!hasPreviousPage}
        className={`px-4 py-2 rounded ${
          hasPreviousPage
            ? 'bg-white text-gray-700 hover:bg-gray-50 border'
            : 'bg-gray-200 text-gray-400 cursor-not-allowed'
        }`}
      >
        Previous
      </button>

      {renderPageNumbers()}

      <button
        onClick={() => handlePageChange(page + 1)}
        disabled={!hasNextPage}
        className={`px-4 py-2 rounded ${
          hasNextPage
            ? 'bg-white text-gray-700 hover:bg-gray-50 border'
            : 'bg-gray-200 text-gray-400 cursor-not-allowed'
        }`}
      >
        Next
      </button>
    </div>
  );
};

export default Pagination; 