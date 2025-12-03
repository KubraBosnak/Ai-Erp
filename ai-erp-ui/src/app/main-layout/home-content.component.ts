import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { Router, RouterModule } from '@angular/router';

interface ModuleCard {
  title: string;
  description: string;
  badge: string;
}

@Component({
  selector: 'app-home-content',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './home-content.component.html',
  styleUrl: './home-content.component.css'
})
export class HomeContentComponent {
  private readonly router = inject(Router);

  protected readonly moduleCards: ModuleCard[] = [
    {
      title: 'Satın Alma Modülü',
      description: 'Tedarik süreçlerini uçtan uca yönetin, tekliften teslimata kadar kontrol sağlayın.',
      badge: 'Pro'
    },
    {
      title: 'Süreç Yönetimi Modülü',
      description: 'İş akışlarını modelleyin, izleyin ve otomatikleştirerek verimliliği artırın.',
      badge: 'Workflow'
    },
    {
      title: 'Bütçe Modülü',
      description: 'Planlanan ve gerçekleşen bütçeleri anlık takip edin, sapmaları erken yakalayın.',
      badge: 'Finance'
    },
    {
      title: 'İnsan Kaynakları Modülü',
      description: 'Çalışan yaşam döngüsünü tek noktadan yönetin, performans ve izin süreçlerini hızlandırın.',
      badge: 'HR'
    },
    {
      title: 'Muhasebe Modülü',
      description: 'Tüm finansal kayıtlarınızı güvenle tutun, mevzuata tam uyum sağlayın.',
      badge: 'Ledger'
    },
    {
      title: 'Lojistik Modülü',
      description: 'Depo ve dağıtım operasyonlarını gerçek zamanlı görünürlük ile optimize edin.',
      badge: 'Logistics'
    }
  ];

  protected handleModuleClick(module: ModuleCard): void {
    if (module.title === 'Satın Alma Modülü') {
      this.router.navigate(['/purchasing']);
    }
  }
}

