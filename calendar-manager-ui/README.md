# AI Calendar Manager - Angular Frontend

A modern Angular application for managing Google Calendar through OAuth authentication and AI-powered natural language commands.

## 🚀 Features

- **Google OAuth Integration** - Secure authentication with Google Calendar
- **Modern Angular 17+** - Latest Angular features with standalone components
- **Responsive Design** - Mobile-first design that works on all devices
- **Real-time Auth State** - Live authentication status monitoring
- **Token Management** - Automatic token refresh and secure storage
- **TypeScript** - Full type safety throughout the application

## 📁 Project Structure

```
src/
├── app/
│   ├── components/
│   │   ├── login/           # Login component with Google OAuth
│   │   └── dashboard/       # Main dashboard after authentication
│   ├── services/
│   │   ├── auth.ts          # Authentication service
│   │   └── api.ts           # API communication service
│   ├── models/
│   │   ├── auth.models.ts   # Authentication type definitions
│   │   └── calendar.models.ts # Calendar type definitions
│   ├── guards/
│   │   └── auth.guard.ts    # Route protection guard
│   └── environments/
│       ├── environment.ts   # Development environment
│       └── environment.prod.ts # Production environment
```

## 🛠️ Development Setup

### Prerequisites
- Node.js 20+ or 22+
- Angular CLI 17+
- Running backend API (CalendarManager.API)

### Installation
```bash
npm install
```

### Development Server
```bash
ng serve
```
Navigate to `http://localhost:4200`

### Build
```bash
ng build
```

### Production Build
```bash
ng build --configuration production
```

## 🔧 Configuration

### Environment Variables
Update `src/environments/environment.ts`:

```typescript
export const environment = {
  production: false,
  apiUrl: 'http://localhost:5047'  // Your API URL
};
```

For production, update `src/environments/environment.prod.ts`:

```typescript
export const environment = {
  production: true,
  apiUrl: 'https://your-api-domain.com'
};
```

## 🎯 Key Components

### Authentication Service (`services/auth.ts`)
- Manages OAuth flow with backend API
- Provides reactive authentication state
- Handles login/logout operations
- Automatic token validation

### Login Component (`components/login/`)
- Beautiful Google OAuth login interface
- Loading states and error handling
- Responsive design with animations

### Dashboard Component (`components/dashboard/`)
- Post-authentication interface
- User information display
- Action buttons for testing functionality
- Feature preview cards

### Auth Guard (`guards/auth.guard.ts`)
- Protects routes requiring authentication
- Redirects unauthenticated users to login

## 🔐 Authentication Flow

1. User visits protected route
2. Auth guard checks authentication status
3. If not authenticated, redirects to `/login`
4. User clicks "Connect Google Calendar"
5. Redirects to Google OAuth
6. Google redirects back with authorization code
7. Backend exchanges code for tokens
8. Frontend updates authentication state
9. User gains access to protected routes

## 🎨 Styling

- **SCSS** - Structured stylesheets with variables
- **Responsive Design** - Mobile-first approach
- **Modern UI** - Clean, professional interface
- **Accessibility** - WCAG compliant components

## 🔌 API Integration

The frontend communicates with the .NET backend API:

- `GET /api/auth/status` - Check authentication status
- `GET /api/auth/google-login` - Get OAuth URL
- `POST /api/auth/logout` - Logout and revoke tokens
- `POST /api/auth/test-token` - Validate access token

## 🚀 Deployment

### Development
```bash
ng serve --host 0.0.0.0 --port 4200
```

### Production
1. Build for production:
   ```bash
   ng build --configuration production
   ```

2. Serve the `dist/calendar-manager-ui` folder using:
   - Nginx
   - Apache  
   - Node.js static server
   - CDN (CloudFront, Cloudflare)

## 🧪 Testing

### Run Unit Tests
```bash
ng test
```

### Run E2E Tests
```bash
ng e2e
```

## 📝 Notes

- Uses Angular 17+ standalone components
- Implements reactive forms and observables
- Follows Angular best practices
- Optimized for performance and accessibility
- Ready for production deployment
