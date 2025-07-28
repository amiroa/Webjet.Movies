import React from 'react';
import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import MovieListPage from './pages/MovieListPage';
import MovieDetailsPage from './pages/MovieDetailsPage';
import './App.css';

function App() {
  return (
    <Router>
      <div className="App">
        <Routes>
          <Route path="/" element={<MovieListPage />} />
          <Route path="/movie/:title" element={<MovieDetailsPage />} />
        </Routes>
      </div>
    </Router>
  );
}

export default App;
