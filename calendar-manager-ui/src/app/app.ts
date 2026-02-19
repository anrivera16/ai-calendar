import { Component, signal, OnInit } from '@angular/core';
import { Router, RouterOutlet } from '@angular/router';
import { AuthService } from './services/auth';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App implements OnInit {
  protected readonly title = signal('calendar-manager-ui');
  
  constructor(
    private authService: AuthService,
    private router: Router
  ) {}
  
  ngOnInit() {
    // Handle OAuth callback if present
    const callbackResult = this.authService.handleCallback();
    
    if (callbackResult.success !== false) {
      if (callbackResult.success) {
        // Show success message and redirect to dashboard
        setTimeout(() => {
          alert(`✅ ${callbackResult.message || 'Successfully authenticated with Google!'}`);
          this.router.navigate(['/dashboard']);
        }, 500);
      } else if (callbackResult.message) {
        // Show error message and redirect to login
        setTimeout(() => {
          alert(`❌ ${callbackResult.message}`);
          this.router.navigate(['/login']);
        }, 500);
      }
    }
  }
}
