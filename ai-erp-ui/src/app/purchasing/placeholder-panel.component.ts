import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-purchasing-placeholder',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="placeholder-panel">
      <p class="eyebrow">Yapılandırma aşamasında</p>
      <h2>{{ title }}</h2>
      <p>
        Bu sekme için detaylı ekranlar yakında burada olacak. Operasyonel süreçlerinize özel
        gereksinimleri paylaşmak için çözüm ekibimizle iletişime geçebilirsiniz.
      </p>
    </div>
  `,
  styles: [
    `
      .placeholder-panel {
        min-height: 320px;
        display: flex;
        flex-direction: column;
        justify-content: center;
        text-align: center;
        gap: 12px;
        color: #475467;
      }
      .placeholder-panel h2 {
        margin: 0;
        color: #0f172a;
      }
      .eyebrow {
        text-transform: uppercase;
        letter-spacing: 0.3em;
        font-size: 0.75rem;
        color: #2563eb;
      }
    `
  ]
})
export class PurchasingPlaceholderComponent {
  @Input() title = 'Modül';
}

