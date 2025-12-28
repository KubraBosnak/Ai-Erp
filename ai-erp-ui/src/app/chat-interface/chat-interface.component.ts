import { Component, signal, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { ChatService } from '../services/chat.service';

interface Message {
  id: number;
  content: string;
  sender: 'user' | 'bot';
  timestamp: Date;
  isJson?: boolean;
  jsonData?: any;
}

@Component({
  selector: 'app-chat-interface',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './chat-interface.component.html',
  styleUrl: './chat-interface.component.css'
})
export class ChatInterfaceComponent {
  private readonly chatService = inject(ChatService);
  
  newMessage = signal('');
  
  messages = signal<Message[]>([
    {
      id: 1,
      content: 'Merhaba! Size nasıl yardımcı olabilirim?',
      sender: 'bot',
      timestamp: new Date()
    }
  ]);

  // Template'de Array kontrolü için
  isArray(obj: any): boolean {
    return Array.isArray(obj);
  }

  sendMessage(): void {
    const message = this.newMessage().trim();
    if (!message) return;

    // 1. Kullanıcı mesajını ekle
    const newUserMessage: Message = {
      id: Date.now(), // Benzersiz ID için timestamp kullandım
      content: message,
      sender: 'user',
      timestamp: new Date()
    };
    
    this.messages.update(msgs => [...msgs, newUserMessage]);
    this.newMessage.set('');
    
    // 2. API'ye istek gönder
    this.chatService.sendMessage(message).subscribe({
      next: (response: any) => {
        // Backend'den dönen yapı: { data: [...], analysis: "...", generatedSql: "..." }

        // A) Önce Tablo Verisini Kontrol Et ve Ekle
        if (response.data && Array.isArray(response.data) && response.data.length > 0) {
          const tableMessage: Message = {
            id: Date.now() + 1,
            content: 'Veri Sonuçları',
            sender: 'bot',
            timestamp: new Date(),
            isJson: true,
            jsonData: response.data
          };
          this.messages.update(msgs => [...msgs, tableMessage]);
        }

        // B) Hemen Ardından Analiz/Yorum Mesajını Ekle
        if (response.analysis) {
          const analysisMessage: Message = {
            id: Date.now() + 2,
            content: response.analysis, // HTML içerikli metin
            sender: 'bot',
            timestamp: new Date(),
            isJson: false // Bu bir metin (HTML) mesajıdır
          };
          this.messages.update(msgs => [...msgs, analysisMessage]);
        }

        // C) Eğer ne veri ne de analiz varsa (Hata veya boş cevap durumu)
        if (!response.data && !response.analysis) {
             const defaultMessage: Message = {
                id: Date.now() + 3,
                content: response.message || 'Bir sonuç bulunamadı.',
                sender: 'bot',
                timestamp: new Date()
             };
             this.messages.update(msgs => [...msgs, defaultMessage]);
        }
      },
      error: (error) => {
        console.error('Chat API hatası:', error);
        
        const errorMessage: Message = {
          id: Date.now(),
          content: 'Üzgünüm, bir hata oluştu. Lütfen tekrar deneyin.',
          sender: 'bot',
          timestamp: new Date()
        };
        
        this.messages.update(msgs => [...msgs, errorMessage]);
      }
    });
  }

  // --- Yardımcı Format Fonksiyonları ---

  formatTime(date: Date): string {
    return new Date(date).toLocaleTimeString('tr-TR', { 
      hour: '2-digit', 
      minute: '2-digit' 
    });
  }

  formatDate(date: Date): string {
    const today = new Date();
    const messageDate = new Date(date);
    const yesterday = new Date(today);
    yesterday.setDate(yesterday.getDate() - 1);

    if (messageDate.toDateString() === today.toDateString()) {
      return 'Bugün';
    } else if (messageDate.toDateString() === yesterday.toDateString()) {
      return 'Dün';
    } else {
      return messageDate.toLocaleDateString('tr-TR', { 
        day: 'numeric', 
        month: 'short' 
      });
    }
  }

  getJsonKeys(data: any): string[] {
    if (Array.isArray(data) && data.length > 0) {
      return Object.keys(data[0]);
    }
    return Object.keys(data || {});
  }

  formatKey(key: string): string {
    return key
      .replace(/([A-Z])/g, ' $1')
      .replace(/^./, str => str.toUpperCase())
      .trim();
  }
// Eski formatValue fonksiyonunu sil, bunu yapıştır:
  formatValue(key: string, value: any): string {
    if (value === null || value === undefined) {
      return '-';
    }

    // Eğer değer bir sayıysa
    if (typeof value === 'number') {
      // 1. Türkçe formatına çevir (Noktalı binlik ayracı)
      const formattedNumber = value.toLocaleString('tr-TR', { maximumFractionDigits: 2 });

      // 2. Sütun adına bak, para birimi içeriyorsa TL ekle
      const lowerKey = key.toLowerCase();
      if (
        lowerKey.includes('amount') || 
        lowerKey.includes('price') || 
        lowerKey.includes('total') || 
        lowerKey.includes('revenue') || 
        lowerKey.includes('ciro') || 
        lowerKey.includes('tutar') || 
        lowerKey.includes('fiyat')
      ) {
        return `${formattedNumber} TL`;
      }
      
      return formattedNumber;
    }

    // Nesne ise JSON string yap
    if (typeof value === 'object') {
      return JSON.stringify(value);
    }

    // Düz yazıysa olduğu gibi döndür
    return String(value);
  }
}