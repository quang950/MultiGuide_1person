/**
 * MultiGuide - Heritage Modal Service (FE-6.3)
 * Module quản lý hộp thoại thông báo trung tâm chuẩn phong cách di sản vàng đồng (#C28E58),
 * hỗ trợ tái sử dụng cho:
 * 1. Thông báo tính năng đang hoàn thiện / Sắp ra mắt (showComingSoon)
 * 2. Hộp thoại xác nhận thay thế window.confirm (confirm)
 * 3. Hộp thoại thông báo chung thay thế window.alert (alert)
 */
import { store } from './state.js';

class ModalService {
  constructor() {
    this.container = null;
    this.resolvePromise = null;
  }

  /**
   * Tạo hoặc lấy container modal trong DOM
   */
  ensureContainer() {
    if (this.container && document.body.contains(this.container)) {
      return this.container;
    }

    let existing = document.getElementById('heritageModalContainer');
    if (!existing) {
      existing = document.createElement('div');
      existing.id = 'heritageModalContainer';
      existing.className = 'heritage-modal-backdrop';
      existing.setAttribute('aria-hidden', 'true');
      document.body.appendChild(existing);
    }

    this.container = existing;
    return this.container;
  }

  /**
   * Đóng modal
   */
  close(result = false) {
    if (!this.container) return;

    const card = this.container.querySelector('.heritage-modal-card');
    if (card) {
      card.classList.remove('is-active');
    }
    this.container.classList.remove('is-visible');
    this.container.setAttribute('aria-hidden', 'true');

    if (this.resolvePromise) {
      this.resolvePromise(result);
      this.resolvePromise = null;
    }

    setTimeout(() => {
      if (this.container && !this.container.classList.contains('is-visible')) {
        this.container.innerHTML = '';
      }
    }, 250);
  }

  /**
   * Hiển thị hộp thoại Sắp Ra Mắt / Đang Hoàn Thiện (Coming Soon)
   */
  showComingSoon(options = {}) {
    const currentLang = store.get().selectedLang || 'vi';

    const defaultBadge = options.badge || (
      currentLang === 'en' ? 'COMING SOON' : (currentLang === 'ja' ? '準備中' : 'SẮP RA MẮT')
    );
    const defaultTitle = options.title || (
      currentLang === 'en' ? 'Feature in Progress' : (currentLang === 'ja' ? '開発中の機能' : 'Tính Năng Đang Hoàn Thiện')
    );
    const defaultMessage = options.message || (
      currentLang === 'en'
        ? 'Chinese translation and audio narration are currently in development by our heritage team and will be released soon!'
        : currentLang === 'ja'
        ? '中国語の翻訳および音声ガイドは現在準備中です。近日公開予定ですのでお楽しみに！'
        : 'Bản dịch và giọng đọc thuyết minh tiếng Trung đang được đội ngũ di sản hoàn thiện và sẽ sớm ra mắt du khách!'
    );
    const defaultConfirmText = options.confirmText || (
      currentLang === 'en' ? 'Got It' : (currentLang === 'ja' ? '了解' : 'Đã hiểu')
    );
    const icon = options.icon || '🇨🇳';

    return this.render({
      badge: defaultBadge,
      title: defaultTitle,
      message: defaultMessage,
      icon,
      confirmText: defaultConfirmText,
      showCancel: false,
      theme: 'gold'
    });
  }

  /**
   * Hộp thoại xác nhận hành động (Thay thế window.confirm thô sơ)
   */
  confirm(options = {}) {
    const currentLang = store.get().selectedLang || 'vi';
    const defaultTitle = options.title || (
      currentLang === 'en' ? 'Exit Pass Confirmation' : (currentLang === 'ja' ? 'パス終了の確認' : (currentLang === 'zh' ? '退出通行证确认' : 'Xác Nhận Thoát Vé'))
    );
    const defaultMessage = options.message || (
      currentLang === 'en' ? 'Do you want to exit your active pass and return to the welcome screen?' : (currentLang === 'ja' ? 'パスを終了して最初のようこそ画面に戻りますか？' : (currentLang === 'zh' ? '您确定要退出当前通行证并返回欢迎页面吗？' : 'Bạn có muốn đăng xuất vé và quay lại màn hình chào đón ban đầu không?'))
    );
    const defaultConfirmText = options.confirmText || (
      currentLang === 'en' ? 'Exit' : (currentLang === 'ja' ? '終了する' : (currentLang === 'zh' ? '确定' : 'Đồng ý'))
    );
    const defaultCancelText = options.cancelText || (
      currentLang === 'en' ? 'Cancel' : (currentLang === 'ja' ? 'キャンセル' : (currentLang === 'zh' ? '取消' : 'Hủy'))
    );
    const icon = options.icon || '🏛️';

    return this.render({
      badge: options.badge || null,
      title: defaultTitle,
      message: defaultMessage,
      icon,
      confirmText: defaultConfirmText,
      cancelText: defaultCancelText,
      showCancel: true,
      theme: options.theme || 'gold'
    });
  }

  /**
   * Hộp thoại thông báo chung (Thay thế window.alert)
   */
  alert(options = {}) {
    const currentLang = store.get().selectedLang || 'vi';
    const defaultConfirmText = options.confirmText || (
      currentLang === 'en' ? 'OK' : (currentLang === 'ja' ? '閉じる' : 'Đóng')
    );
    const icon = options.icon || '✨';

    return this.render({
      badge: options.badge || null,
      title: options.title || '',
      message: options.message || '',
      icon,
      confirmText: defaultConfirmText,
      showCancel: false,
      theme: options.theme || 'gold'
    });
  }

  /**
   * Render hộp thoại vào DOM và kích hoạt hiệu ứng mượt mà
   */
  render({ badge, title, message, icon, confirmText, cancelText, showCancel, theme }) {
    const container = this.ensureContainer();

    const badgeHtml = badge ? `<div class="modal-badge">${badge}</div>` : '';
    const cancelBtnHtml = showCancel
      ? `<button type="button" class="btn-modal-cancel" id="btnModalCancel">${cancelText}</button>`
      : '';

    container.innerHTML = `
      <div class="heritage-modal-card theme-${theme}">
        <div class="modal-icon-wrapper">
          <div class="modal-icon-circle">
            <span class="modal-icon-symbol">${icon}</span>
          </div>
        </div>

        <div class="modal-content-group">
          ${badgeHtml}
          <h3 class="modal-title">${title}</h3>
          <p class="modal-message">${message}</p>
        </div>

        <div class="modal-actions-group ${showCancel ? 'has-two-buttons' : ''}">
          ${cancelBtnHtml}
          <button type="button" class="btn-modal-confirm" id="btnModalConfirm">${confirmText}</button>
        </div>
      </div>
    `;

    return new Promise((resolve) => {
      this.resolvePromise = resolve;

      container.classList.add('is-visible');
      container.setAttribute('aria-hidden', 'false');

      const card = container.querySelector('.heritage-modal-card');
      requestAnimationFrame(() => {
        if (card) card.classList.add('is-active');
      });

      // Bắt sự kiện nút xác nhận
      const btnConfirm = container.querySelector('#btnModalConfirm');
      if (btnConfirm) {
        btnConfirm.addEventListener('click', () => this.close(true));
      }

      // Bắt sự kiện nút hủy
      const btnCancel = container.querySelector('#btnModalCancel');
      if (btnCancel) {
        btnCancel.addEventListener('click', () => this.close(false));
      }

      // Bấm ra ngoài nền mờ để đóng
      container.onclick = (e) => {
        if (e.target === container) {
          this.close(false);
        }
      };

      // Nhấn ESC để đóng
      const onKeyDown = (e) => {
        if (e.key === 'Escape') {
          window.removeEventListener('keydown', onKeyDown);
          this.close(false);
        }
      };
      window.addEventListener('keydown', onKeyDown, { once: true });
    });
  }
}

export const modalService = new ModalService();
window.modalService = modalService;

