import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { DataViewService } from '../services/data-view.service';

@Component({
  selector: 'app-raw-data-table',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './raw-data-table.component.html',
  styleUrl: './raw-data-table.component.css'
})
export class RawDataTableComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly dataViewService = inject(DataViewService);

  protected readonly isLoading = signal(true);
  protected readonly tableData = signal<any[]>([]);
  protected readonly tableColumns = signal<string[]>([]);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly tableName = signal<string>('');

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      const tableName = params.get('tableName');
      if (tableName) {
        this.tableName.set(tableName);
        this.loadData(tableName);
      }
    });
  }

  protected getTableTitle(): string {
    const name = this.tableName();
    const titleMap: { [key: string]: string } = {
      'products': 'Ürünler',
      'vendors': 'Tedarikçiler',
      'orders': 'Siparişler',
      'details': 'Detaylar',
      'receipts': 'Fişler'
    };
    return titleMap[name] || this.formatKey(name);
  }

  private loadData(tableName: string): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.dataViewService.getData(tableName).subscribe({
      next: (response) => {
        const data = Array.isArray(response) ? response : (response.data || []);
        
        if (data.length > 0) {
          this.tableColumns.set(Object.keys(data[0]));
          this.tableData.set(data);
        } else {
          this.tableData.set([]);
          this.tableColumns.set([]);
        }
        
        this.isLoading.set(false);
      },
      error: (error) => {
        console.error('Veri yükleme hatası:', error);
        this.errorMessage.set('Veriler yüklenirken bir hata oluştu.');
        this.isLoading.set(false);
      }
    });
  }

  protected formatKey(key: string): string {
    return key
      .replace(/([A-Z])/g, ' $1')
      .replace(/^./, str => str.toUpperCase())
      .trim();
  }

  protected formatValue(value: any): string {
    if (value === null || value === undefined) {
      return '-';
    }
    if (typeof value === 'object') {
      return JSON.stringify(value);
    }
    return String(value);
  }
}

