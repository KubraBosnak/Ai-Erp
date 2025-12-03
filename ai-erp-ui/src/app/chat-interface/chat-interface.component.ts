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

interface Chat {
  id: number;
  title: string;
  lastMessage: string;
  lastMessageTime: Date;
  unreadCount: number;
  messages: Message[];
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
  
  currentChatId = signal<number | null>(null);
  isSidebarOpen = signal(false);
  
  chats = signal<Chat[]>([
    {
      id: 1,
      title: 'Destek Ekibi',
      lastMessage: 'Merhaba, size nasıl yardımcı olabilirim?',
      lastMessageTime: new Date(2024, 0, 15, 14, 30),
      unreadCount: 2,
      messages: [
        {
          id: 1,
          content: 'Merhaba, size nasıl yardımcı olabilirim?',
          sender: 'bot',
          timestamp: new Date(2024, 0, 15, 14, 30)
        },
        {
          id: 2,
          content: 'Sipariş durumumu öğrenmek istiyorum',
          sender: 'user',
          timestamp: new Date(2024, 0, 15, 14, 32)
        },
        {
          id: 3,
          content: 'Sipariş numaranızı paylaşabilir misiniz?',
          sender: 'bot',
          timestamp: new Date(2024, 0, 15, 14, 33)
        }
      ]
    },
    {
      id: 2,
      title: 'Teknik Destek',
      lastMessage: 'Sorun çözüldü mü?',
      lastMessageTime: new Date(2024, 0, 14, 10, 15),
      unreadCount: 0,
      messages: [
        {
          id: 1,
          content: 'Teknik bir sorun yaşıyorum',
          sender: 'user',
          timestamp: new Date(2024, 0, 14, 10, 10)
        },
        {
          id: 2,
          content: 'Lütfen sorununuzu detaylı açıklayın',
          sender: 'bot',
          timestamp: new Date(2024, 0, 14, 10, 12)
        },
        {
          id: 3,
          content: 'Sorun çözüldü mü?',
          sender: 'bot',
          timestamp: new Date(2024, 0, 14, 10, 15)
        }
      ]
    },
    {
      id: 3,
      title: 'Satış Ekibi',
      lastMessage: 'Teşekkür ederim',
      lastMessageTime: new Date(2024, 0, 13, 16, 45),
      unreadCount: 0,
      messages: [
        {
          id: 1,
          content: 'Ürün hakkında bilgi almak istiyorum',
          sender: 'user',
          timestamp: new Date(2024, 0, 13, 16, 40)
        },
        {
          id: 2,
          content: 'Tabii ki, hangi ürünü merak ediyorsunuz?',
          sender: 'bot',
          timestamp: new Date(2024, 0, 13, 16, 42)
        },
        {
          id: 3,
          content: 'Teşekkür ederim',
          sender: 'user',
          timestamp: new Date(2024, 0, 13, 16, 45)
        }
      ]
      
    }
    
  ]);
// Bu method, global Array.isArray komutunu template'e tanıtmak için var.
  isArray(obj: any): boolean {
    return Array.isArray(obj);
  }
  get currentChat(): Chat | null {
    const chatId = this.currentChatId();
    if (chatId === null) return null;
    return this.chats().find(chat => chat.id === chatId) || null;
  }

  selectChat(chatId: number): void {
    this.currentChatId.set(chatId);
    const chat = this.chats().find(c => c.id === chatId);
    if (chat) {
      chat.unreadCount = 0;
    }
    // Sohbet seçildiğinde sidebar'ı kapat
    this.isSidebarOpen.set(false);
  }

  toggleSidebar(): void {
    this.isSidebarOpen.update(state => !state);
  }

  sendMessage(): void {
    const message = this.newMessage().trim();
    if (!message || !this.currentChatId()) return;

    const chatId = this.currentChatId()!;
    const chats = this.chats();
    const chatIndex = chats.findIndex(c => c.id === chatId);
    
    if (chatIndex !== -1) {
      const newMessage: Message = {
        id: chats[chatIndex].messages.length + 1,
        content: message,
        sender: 'user',
        timestamp: new Date()
      };
      
      chats[chatIndex].messages.push(newMessage);
      chats[chatIndex].lastMessage = message;
      chats[chatIndex].lastMessageTime = new Date();
      
      this.chats.set([...chats]);
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
            id: chats[chatIndex].messages.length + 1,
            content: isJson ? 'JSON veri' : responseString,
            sender: 'bot',
            timestamp: new Date(),
            isJson: isJson,
            jsonData: isJson ? parsedJson : undefined
          };
          
          chats[chatIndex].messages.push(botMessage);
          chats[chatIndex].lastMessage = isJson ? 'Tablo verisi' : responseString.substring(0, 50) + (responseString.length > 50 ? '...' : '');
          chats[chatIndex].lastMessageTime = new Date();
          
          this.chats.set([...chats]);
        },
        error: (error) => {
          console.error('Chat API hatası:', error);
          
          // Hata durumunda kullanıcıya bilgi ver
          const errorMessage: Message = {
            id: chats[chatIndex].messages.length + 1,
            content: 'Üzgünüm, bir hata oluştu. Lütfen tekrar deneyin.',
            sender: 'bot',
            timestamp: new Date()
          };
          
          chats[chatIndex].messages.push(errorMessage);
          chats[chatIndex].lastMessage = errorMessage.content;
          chats[chatIndex].lastMessageTime = new Date();
          
          this.chats.set([...chats]);
        }
      });
    }
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

