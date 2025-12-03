import { CommonModule } from '@angular/common';
import { Component, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

interface StockRow {
  code: string;
  name: string;
  warehouse: string;
  quantity: number;
  critical: number;
}

type SortField = keyof Pick<StockRow, 'code' | 'name' | 'warehouse' | 'quantity' | 'critical'>;

@Component({
  selector: 'app-stock-monitoring',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './stock-monitoring.component.html',
  styleUrl: './stock-monitoring.component.css'
})
export class StockMonitoringComponent {
  protected readonly query = signal('');
  protected readonly sortField = signal<SortField>('code');
  protected readonly sortDirection = signal<'asc' | 'desc'>('asc');
  protected readonly lastQuery = signal<string | null>(null);

  private readonly data = signal<StockRow[]>([
    { code: 'MAT-001', name: 'Çelik Panel', warehouse: 'İstanbul Merkez', quantity: 42, critical: 25 },
    { code: 'MAT-002', name: 'Bakır Kablo', warehouse: 'İzmir Depo', quantity: 8, critical: 15 },
    { code: 'MAT-003', name: 'Polymer Granül', warehouse: 'Ankara Depo', quantity: 120, critical: 50 },
    { code: 'MAT-004', name: 'Lityum Pil', warehouse: 'İstanbul Merkez', quantity: 6, critical: 12 },
    { code: 'MAT-005', name: 'Ambalaj Kutusu', warehouse: 'Bursa Depo', quantity: 280, critical: 60 },
    { code: 'MAT-006', name: 'Yedek Motor', warehouse: 'İzmir Depo', quantity: 3, critical: 5 },
    { code: 'MAT-007', name: 'Sensör Modülü', warehouse: 'Ankara Depo', quantity: 15, critical: 20 }
  ]);

  protected readonly filteredRows = computed(() => {
    const needle = this.query().toLowerCase().trim();
    const sorted = [...this.data()].sort((a, b) => {
      const field = this.sortField();
      const direction = this.sortDirection();

      const aValue = a[field];
      const bValue = b[field];

      const compare =
        typeof aValue === 'number' && typeof bValue === 'number'
          ? aValue - bValue
          : String(aValue).localeCompare(String(bValue), 'tr');

      return direction === 'asc' ? compare : -compare;
    });

    if (!needle) {
      return sorted;
    }

    return sorted.filter(row =>
      Object.values(row).some(value => String(value).toLowerCase().includes(needle))
    );
  });

  protected triggerQuery(): void {
    this.lastQuery.set(this.query().trim());
  }

  protected sortBy(field: SortField): void {
    if (this.sortField() === field) {
      this.sortDirection.update(dir => (dir === 'asc' ? 'desc' : 'asc'));
    } else {
      this.sortField.set(field);
      this.sortDirection.set('asc');
    }
  }
}

