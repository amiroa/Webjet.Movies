import React, { useState } from 'react';

interface SearchAndSortProps {
  searchTerm: string;
  sortBy: string;
  sortDescending: boolean;
  onSearchChange: (searchTerm: string) => void;
  onSortChange: (sortBy: string, sortDescending: boolean) => void;
}

const SearchAndSort: React.FC<SearchAndSortProps> = ({
  searchTerm,
  sortBy,
  sortDescending,
  onSearchChange,
  onSortChange,
}) => {
  const [inputValue, setInputValue] = useState(searchTerm);

  const handleSearch = () => {
    onSearchChange(inputValue);
  };

  const handleReset = () => {
    setInputValue('');
    onSearchChange('');
    onSortChange('', false);
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter') {
      handleSearch();
    }
  };

  const handleSortChange = (newSortBy: string) => {
    if (newSortBy === sortBy) {
      onSortChange(newSortBy, !sortDescending);
    } else {
      onSortChange(newSortBy, false);
    }
  };

  const getSortIcon = (field: string) => {
    if (sortBy !== field) {
      return '↕️';
    }
    return sortDescending ? '↓' : '↑';
  };

  return (
    <div className="mb-6">
      <div className="flex flex-col sm:flex-row gap-4 items-end">
        {/* Search Input with Button */}
        <div className="flex-1">
          <label htmlFor="search" className="block text-sm font-medium text-gray-700 mb-1">
            Search Movies
          </label>
          <div className="flex">
            <input
              id="search"
              type="text"
              value={inputValue}
              onChange={(e) => setInputValue(e.target.value)}
              onKeyDown={handleKeyDown}
              placeholder="Search by movie title..."
              className="flex-1 px-3 py-2 border border-gray-300 rounded-l-md shadow-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
            />
            <button
              onClick={handleSearch}
              className="px-4 py-2 bg-blue-500 text-white border border-blue-500 rounded-r-md hover:bg-blue-600 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2"
              title="Search"
            >
              <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
              </svg>
            </button>
          </div>
        </div>

        {/* Sort Options */}
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">
            Sort By
          </label>
          <div className="flex gap-2">
            <button
              onClick={() => handleSortChange('title')}
              className={`px-4 py-2 rounded-md border transition-colors ${
                sortBy === 'title'
                  ? 'bg-blue-500 text-white border-blue-500'
                  : 'bg-white text-gray-700 border-gray-300 hover:bg-gray-50'
              }`}
            >
              Title {getSortIcon('title')}
            </button>
            <button
              onClick={() => handleSortChange('year')}
              className={`px-4 py-2 rounded-md border transition-colors ${
                sortBy === 'year'
                  ? 'bg-blue-500 text-white border-blue-500'
                  : 'bg-white text-gray-700 border-gray-300 hover:bg-gray-50'
              }`}
            >
              Year {getSortIcon('year')}
            </button>
          </div>
        </div>

        {/* Reset Button */}
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">
            &nbsp;
          </label>
          <button
            onClick={handleReset}
            className="px-4 py-2 bg-gray-500 text-white rounded-md border border-gray-500 hover:bg-gray-600 focus:outline-none focus:ring-2 focus:ring-gray-500 focus:ring-offset-2 transition-colors"
            title="Reset filters"
          >
            <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
            </svg>
          </button>
        </div>
      </div>
    </div>
  );
};

export default SearchAndSort; 