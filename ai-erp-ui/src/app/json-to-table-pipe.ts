import { Pipe, PipeTransform } from '@angular/core';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser'; // HTML'i güvenli hale getirmek için gerekli

@Pipe({
  name: 'jsonToTable',
  standalone: true // Standalone mimarisi için zorunlu
})
export class JsonToTablePipe implements PipeTransform {
    
    constructor(private sanitizer: DomSanitizer) {} // Sanitizer'ı inject ediyoruz

    transform(value: string | any): SafeHtml | string {
        // Eğer zaten obje geliyorsa string'e çeviriyoruz
        if (typeof value === 'object' && value !== null && !Array.isArray(value)) {
             // AI'dan gelen cevap objeyse, sadece veriyi (data kısmını) alalım
             value = value.data ? JSON.stringify(value.data) : JSON.stringify(value);
        }

        // String değilse, boşsa veya hatalı ise direkt geri dön
        if (!value || typeof value !== 'string' || value.trim().length === 0) {
          return value;
        }

        try {
          const data = JSON.parse(value);

          // Listenin boş olup olmadığını kontrol et
          if (!Array.isArray(data) || data.length === 0) {
            return value; 
          }
          
          // Tablo başlıkları
          const headers = Object.keys(data[0]);
          let html = '<table style="width:100%; border-collapse: collapse; margin-top: 10px;">';
          
          // Başlık satırı
          html += '<thead><tr style="background-color: #f0f0f0;">';
          headers.forEach(header => {
            html += `<th style="border: 1px solid #ddd; padding: 8px;">${header}</th>`;
          });
          html += '</tr></thead>';

          // Veri satırları
          html += '<tbody>';
          data.forEach(row => {
            html += '<tr>';
            headers.forEach(header => {
              html += `<td style="border: 1px solid #ddd; padding: 8px;">${row[header]}</td>`;
            });
            html += '</tr>';
          });
          html += '</tbody></table>';

          // Angular'a bunun güvenli HTML olduğunu söylüyoruz
          return this.sanitizer.bypassSecurityTrustHtml(html); 

        } catch (e) {
          // JSON parse edilemezse ham metni döndür (Örn: "Bir hata oluştu" gibi mesajlar)
          return value;
        }
    }
}