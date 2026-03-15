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

    if (callbackResult.success) {
      // Auth status is already set synchronously by handleCallback — navigate immediately
      this.router.navigate(['/dashboard']);
    } else if (callbackResult.message) {
      // Show error and redirect to login
      console.error('OAuth error:', callbackResult.message);
      this.router.navigate(['/login']);
    }
  }
}
