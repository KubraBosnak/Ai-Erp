import { Injectable, inject, signal } from '@angular/core';
import { Router } from '@angular/router';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly router = inject(Router);
  
  private readonly _isLoggedIn = signal(false);
  private readonly _currentUser = signal<string | null>(null);

  readonly isLoggedIn = this._isLoggedIn.asReadonly();
  readonly currentUser = this._currentUser.asReadonly();

  login(username: string, password: string): boolean {
    const success = username === 'kubrab' && password === '123';

    if (success) {
      this._isLoggedIn.set(true);
      this._currentUser.set(username);
    } else {
      this._isLoggedIn.set(false);
      this._currentUser.set(null);
    }

    return success;
  }

  logout(): void {
    this._isLoggedIn.set(false);
    this._currentUser.set(null);
    this.router.navigate(['/login']);
  }
}

