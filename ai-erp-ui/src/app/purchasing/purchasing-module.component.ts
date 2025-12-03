import { CommonModule, DOCUMENT } from '@angular/common';
import { Component, signal, effect, inject } from '@angular/core';
import { RouterModule, Router, NavigationEnd } from '@angular/router';
import { filter } from 'rxjs';

interface PurchasingTab {
  label: string;
  path: string;
  description: string;
}

interface DataMenuItem {
  label: string;
  path: string;
}

@Component({
  selector: 'app-purchasing-module',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './purchasing-module.component.html',
  styleUrl: './purchasing-module.component.css'
})
export class PurchasingModuleComponent {
  private readonly document = inject(DOCUMENT);
  private readonly router = inject(Router);

  protected readonly isDataMenuOpen = signal(false);
  protected readonly isTransactionsActive = signal(false);

  protected readonly dataMenuItems: DataMenuItem[] = [
    { label: 'Ürünler', path: 'data/products' },
    { label: 'Tedarikçiler', path: 'data/vendors' },
    { label: 'Siparişler', path: 'data/orders' },
    { label: 'Detaylar', path: 'data/details' },
    { label: 'Fişler', path: 'data/receipts' }
  ];

  protected readonly tabs: PurchasingTab[] = [
    { label: 'Depo İşlemleri', path: 'warehouse', description: 'Stok ile depo görünürlüğü' },
    { label: 'Tedarikçi İşlemleri', path: 'vendor', description: 'Tedarikçi ilişkileri' },
    { label: 'Lojistik İşlemleri', path: 'logistics', description: 'Nakliye ve dağıtım' },
    { label: 'Tanımlar', path: 'definitions', description: 'Parametre ve sözlükler' }
  ];

  constructor() {
    // Dropdown dışına tıklandığında kapat
    effect(() => {
  if (this.isDataMenuOpen()) {
    const handler = (event: MouseEvent) => {
      const target = event.target as HTMLElement;
      if (!target.closest('.tab-btn-dropdown')) {
        this.closeDataMenu();
      }
    };

    this.document.addEventListener('click', handler);

    // Cleanup fonksiyonu
    return () => {
      this.document.removeEventListener('click', handler);
    };
  }

  // Menü kapalıysa yine return etmek zorundayız
  return;
});

    // Router değişikliklerini dinle
    this.router.events
      .pipe(filter(event => event instanceof NavigationEnd))
      .subscribe(() => {
        const url = this.router.url;
        this.isTransactionsActive.set(
          url.includes('/purchasing/transactions') || 
          url.includes('/purchasing/data/')
        );
      });
    
    // İlk yüklemede kontrol et
    const url = this.router.url;
    this.isTransactionsActive.set(
      url.includes('/purchasing/transactions') || 
      url.includes('/purchasing/data/')
    );
  }

  toggleDataMenu(event?: Event): void {
    if (event) {
      event.stopPropagation();
    }
    this.isDataMenuOpen.update(state => !state);
  }

  closeDataMenu(): void {
    this.isDataMenuOpen.set(false);
  }
}

