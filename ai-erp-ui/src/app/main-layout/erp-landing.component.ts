import { CommonModule } from '@angular/common';
import { Component, HostListener, inject, signal } from '@angular/core';
import { Router, RouterModule, RouterOutlet } from '@angular/router';

import { ChatInterfaceComponent } from '../chat-interface/chat-interface.component';
import { AuthService } from '../core/auth.service';

type SectionId = 'anasayfa' | 'moduller' | 'hakkimizda' | 'referanslar' | 'iletisim';

@Component({
  selector: 'app-erp-landing',
  standalone: true,
  imports: [CommonModule, RouterModule, RouterOutlet, ChatInterfaceComponent],
  templateUrl: './erp-landing.component.html',
  styleUrl: './erp-landing.component.css'
})
export class ErpLandingComponent {
  protected readonly router = inject(Router);
  protected readonly authService = inject(AuthService);

  protected readonly navItems: { id: SectionId; label: string; route?: string }[] = [
    { id: 'anasayfa', label: 'ANASAYFA', route: '/home' },
    { id: 'moduller', label: 'MODÜLLER', route: '/modules' },
    { id: 'hakkimizda', label: 'HAKKIMIZDA' },
    { id: 'referanslar', label: 'REFERANSLAR' },
    { id: 'iletisim', label: 'İLETİŞİM' }
  ];

  protected readonly isChatOpen = signal(false);
  protected readonly isProfileMenuOpen = signal(false);

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    const target = event.target as HTMLElement;
    if (!target.closest('.profile-menu-wrapper')) {
      this.isProfileMenuOpen.set(false);
    }
  }

  protected navigateToSection(item: { id: SectionId; label: string; route?: string }): void {
    if (item.route) {
      this.router.navigate([item.route]);
    }
  }

  protected handleLogout(): void {
    this.authService.logout();
    this.isProfileMenuOpen.set(false);
  }

  protected toggleChat(forceState?: boolean): void {
    if (typeof forceState === 'boolean') {
      this.isChatOpen.set(forceState);
      return;
    }
    this.isChatOpen.update(state => !state);
  }

  protected toggleProfileMenu(): void {
    this.isProfileMenuOpen.update(state => !state);
  }
}

