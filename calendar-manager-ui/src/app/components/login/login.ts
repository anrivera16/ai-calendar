import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
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
  isReauth = false;

  constructor(private authService: AuthService, private route: ActivatedRoute) {
    this.loading$ = this.authService.loading$;
    this.isAuthenticated$ = this.authService.isAuthenticated$;
  }

  ngOnInit() {
    this.isReauth = this.route.snapshot.queryParamMap.get('reauth') === 'true';
    // Don't auto-initiate login on reauth — let user click to avoid redirect loops
  }

  onLogin() {
    this.authService.initiateLogin();
  }
}
