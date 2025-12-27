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

  // Bu method, global Array.isArray komutunu template'e tanıtmak için var.
  isArray(obj: any): boolean {
    return Array.isArray(obj);
  }

  sendMessage(): void {
    const message = this.newMessage().trim();
    if (!message) return;

    const newUserMessage: Message = {
      id: this.messages().length + 1,
      content: message,
      sender: 'user',
      timestamp: new Date()
    };
    
    this.messages.update(msgs => [...msgs, newUserMessage]);
    this.newMessage.set('');
    
    // API'ye istek gönder
    this.chatService.sendMessage(message).subscribe({
      next: (response) => {
        const botResponse = response.data || response;
        
        // JSON kontrolü
        let parsedJson: any = null;
        let isJson = false;
        let responseString = '';
        
        // Eğer zaten bir obje/array ise
        if (typeof botResponse === 'object' && botResponse !== null) {
          parsedJson = botResponse;
          isJson = true;
          responseString = JSON.stringify(botResponse);
        } else if (typeof botResponse === 'string') {
          // String ise JSON olup olmadığını kontrol et
          responseString = botResponse;
          try {
            parsedJson = JSON.parse(botResponse);
            // Eğer parse edilen değer bir obje veya array ise JSON olarak kabul et
            if (typeof parsedJson === 'object' && parsedJson !== null) {
              isJson = true;
            }
          } catch (e) {
            // JSON değil, normal metin
            isJson = false;
          }
        } else {
          responseString = String(botResponse);
          isJson = false;
        }
        
        const botMessage: Message = {
          id: this.messages().length + 1,
          content: isJson ? 'JSON veri' : responseString,
          sender: 'bot',
          timestamp: new Date(),
          isJson: isJson,
          jsonData: isJson ? parsedJson : undefined
        };
        
        this.messages.update(msgs => [...msgs, botMessage]);
      },
      error: (error) => {
        console.error('Chat API hatası:', error);
        
        // Hata durumunda kullanıcıya bilgi ver
        const errorMessage: Message = {
          id: this.messages().length + 1,
          content: 'Üzgünüm, bir hata oluştu. Lütfen tekrar deneyin.',
          sender: 'bot',
          timestamp: new Date()
        };
        
        this.messages.update(msgs => [...msgs, errorMessage]);
      }
    });
  }

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
    // Key'leri daha okunaklı hale getir (örn: stockAmount -> Stock Amount)
    return key
      .replace(/([A-Z])/g, ' $1')
      .replace(/^./, str => str.toUpperCase())
      .trim();
  }

  formatValue(value: any): string {
    if (value === null || value === undefined) {
      return '-';
    }
    if (typeof value === 'object') {
      return JSON.stringify(value);
    }
    return String(value);
  }
}
