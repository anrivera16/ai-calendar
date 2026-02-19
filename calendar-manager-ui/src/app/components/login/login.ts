import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Observable } from 'rxjs';
import { AuthService } from '../../services/auth';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './login.html',
  styleUrl: './login.scss',
})
export class LoginComponent implements OnInit {
  loading$: Observable<boolean>;
  isAuthenticated$: Observable<boolean>;

  constructor(private authService: AuthService) {
    this.loading$ = this.authService.loading$;
    this.isAuthenticated$ = this.authService.isAuthenticated$;
  }

  ngOnInit() {
    // Component initialization
  }

  onLogin() {
    this.authService.initiateLogin().subscribe({
      next: (authUrl) => {
        window.location.href = authUrl;
      },
      error: (error) => {
        console.error('Login failed:', error);
        alert('Login failed: ' + error.message);
      }
    });
  }
}
