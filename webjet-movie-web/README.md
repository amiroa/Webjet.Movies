# Movie Price Comparison App

A React-based frontend for the Movie Price Comparison API that allows users to browse movies and compare prices from different providers.

## Features

- **Movie List View**: Browse all available movies in a responsive grid layout
- **Movie Details**: View detailed information about each movie including plot, cast, and director
- **Price Comparison**: See the cheapest price available and compare prices across providers
- **Responsive Design**: Mobile-friendly interface using Tailwind CSS
- **Error Handling**: Graceful error handling with user-friendly messages
- **Loading States**: Loading spinners and skeleton states for better UX

## Technology Stack

- **React 19** with TypeScript
- **React Router** for client-side routing
- **Axios** for HTTP requests
- **Tailwind CSS** for styling
- **Create React App** for build tooling

## Getting Started

### Prerequisites

- Node.js (v16 or higher)
- npm or yarn
- Backend API running on http://localhost:5000

### Installation

1. Install dependencies:
   ```bash
   npm install
   ```

2. Start the development server:
   ```bash
   npm start
   ```

3. Open [http://localhost:3000](http://localhost:3000) to view it in the browser.

### API Configuration

The app is configured to connect to the backend API at `http://localhost:5000`. You can change this by:

1. Setting the `REACT_APP_API_URL` environment variable
2. Or the app will use the proxy configuration in `package.json`

## Project Structure

```
src/
├── components/        # Reusable UI components
├── pages/             # Page components
├── services/          # API service layer
├── types/             # TypeScript type definitions
├── App.tsx            # Main app component with routing
└── index.css          # Global styles with Tailwind
```

## Available Scripts

- `npm start` - Runs the app in development mode
- `npm run build` - Builds the app for production
- `npm test` - Launches the test runner
- `npm run eject` - Ejects from Create React App (not recommended)

## Deployment

The app can be deployed to any static hosting service:

1. Build the production version:
   ```bash
   npm run build
   ```

2. Deploy the `build` folder to your hosting service