/**
 * eSignCloud Portal - Frontend Application Controller & Slider Engine
 */

class Slider {
  constructor(containerId, wrapperId, dotsId) {
    this.container = document.getElementById(containerId);
    this.wrapper = document.getElementById(wrapperId);
    this.dotsContainer = document.getElementById(dotsId);
    this.slides = [];
    this.currentIndex = 0;
    this.timer = null;
    this.intervalMs = 5000;
  }

  setSlides(slides) {
    this.slides = slides || [];
    this.render();
    this.startAutoPlay();
  }

  render() {
    if (!this.wrapper || this.slides.length === 0) return;

    this.wrapper.innerHTML = this.slides.map((slide, idx) => {
      const hasImage = slide.imageUrl && slide.imageUrl.trim() !== '';
      const fitMode = slide.imageFit || 'contain';
      const bgStyle = hasImage
        ? `background: #0f172a url('${slide.imageUrl}') center / ${fitMode} no-repeat;`
        : `background: ${slide.gradient || 'linear-gradient(135deg, #1e3c72, #2a5298)'};`;

      const link = (slide.linkUrl || slide.buttonLink || '').trim();
      const hasValidLink = link && link !== '#' && link !== '';

      const titleHtml = hasValidLink
        ? `<a href="${link}" target="_blank" rel="noopener noreferrer" title="Mở liên kết: ${link}">${slide.title || ''} <i class="fa-solid fa-arrow-up-right-from-square slide-link-icon"></i></a>`
        : (slide.title || '');

      const hasText = (slide.title && slide.title.trim() !== '') || (slide.description && slide.description.trim() !== '');
      const overlayHtml = hasImage
        ? (hasText ? '<div class="slide-overlay" style="background: linear-gradient(180deg, rgba(15,23,42,0.1) 0%, rgba(15,23,42,0.7) 70%, rgba(15,23,42,0.92) 100%);"></div>' : '')
        : '<div class="slide-overlay"></div>';

      return `
      <div class="slide-item ${idx === this.currentIndex ? 'active' : ''}" style="${bgStyle}">
        ${overlayHtml}
        ${hasText ? `
        <div class="slide-content">
          <span class="slide-tag"><i class="fa-solid fa-shield-halved"></i> ${slide.tag || 'ESIGNCLOUD'}</span>
          <h3 class="slide-title">${titleHtml}</h3>
          <p class="slide-desc">${slide.description || ''}</p>
        </div>
        ` : ''}
      </div>
    `;
    }).join('');

    if (this.dotsContainer) {
      this.dotsContainer.innerHTML = this.slides.map((_, idx) => `
        <div class="slider-dot ${idx === this.currentIndex ? 'active' : ''}" onclick="window.${this.wrapper.id === 'login-slider-wrapper' ? 'sliderLogin' : 'sliderDashboard'}.goTo(${idx})"></div>
      `).join('');
    }

    if (this.container) {
      this.container.onmouseenter = () => this.stopAutoPlay();
      this.container.onmouseleave = () => this.startAutoPlay();
    }
  }

  goTo(index) {
    if (this.slides.length === 0) return;
    this.currentIndex = (index + this.slides.length) % this.slides.length;
    this.updateActiveSlide();
  }

  next() {
    this.goTo(this.currentIndex + 1);
  }

  prev() {
    this.goTo(this.currentIndex - 1);
  }

  updateActiveSlide() {
    const slideEls = this.wrapper.querySelectorAll('.slide-item');
    slideEls.forEach((el, idx) => {
      el.classList.toggle('active', idx === this.currentIndex);
    });

    if (this.dotsContainer) {
      const dotEls = this.dotsContainer.querySelectorAll('.slider-dot');
      dotEls.forEach((el, idx) => {
        el.classList.toggle('active', idx === this.currentIndex);
      });
    }
  }

  startAutoPlay() {
    this.stopAutoPlay();
    this.timer = setInterval(() => this.next(), this.intervalMs);
  }

  stopAutoPlay() {
    if (this.timer) {
      clearInterval(this.timer);
      this.timer = null;
    }
  }
}

// Global Sliders
const sliderLogin = new Slider('login-slider', 'login-slider-wrapper', 'login-slider-dots');
const sliderDashboard = new Slider('dashboard-slider', 'dashboard-slider-wrapper', 'dashboard-slider-dots');

class AppController {
  constructor() {
    this.uid = localStorage.getItem('eSign_uid') || '';
    this.passcode = sessionStorage.getItem('eSign_passcode') || '';
    this.signerName = localStorage.getItem('eSign_signer_name') || '';
    this.isAdmin = sessionStorage.getItem('eSign_is_admin') === 'true';
    this.config = null;
    this.history = [];
    this.selectedFile = null;
  }

  async init() {
    this.setupDropzone();
    await this.loadConfig();

    if (this.uid && this.passcode) {
      this.showDashboard();
    } else {
      this.showLogin();
      if (this.config) {
        document.getElementById('login-uid').value = this.config.defaultAgreementUUID || '';
        document.getElementById('login-passcode').value = this.config.defaultPassCode || '';
      }
    }
  }

  // =================== NAVIGATION & VIEWS ===================
  showLogin() {
    document.getElementById('view-login').classList.add('active');
    document.getElementById('view-dashboard').classList.remove('active');
    document.getElementById('view-settings').classList.remove('active');

    document.getElementById('header-user-badge').style.display = 'none';
    document.getElementById('btn-logout').style.display = 'none';
    document.getElementById('btn-settings').style.display = 'none';
    document.getElementById('badge-admin').style.display = 'none';

    if (document.getElementById('login-uid')) {
      document.getElementById('login-uid').value = this.config?.defaultAgreementUUID || '';
    }
    if (document.getElementById('login-passcode')) {
      document.getElementById('login-passcode').value = this.config?.defaultPassCode || '';
    }
  }

  showDashboard() {
    if (!this.uid || !this.passcode) {
      this.showLogin();
      return;
    }

    document.getElementById('view-login').classList.remove('active');
    document.getElementById('view-dashboard').classList.add('active');
    document.getElementById('view-settings').classList.remove('active');

    document.getElementById('header-user-badge').style.display = 'flex';
    document.getElementById('header-user-name').innerText = this.signerName || 'Chủ Tài Khoản';
    document.getElementById('header-user-uid').innerText = this.uid;
    document.getElementById('header-user-avatar').innerText = (this.signerName ? this.signerName.charAt(0) : (this.uid ? this.uid.charAt(0) : 'U')).toUpperCase();
    document.getElementById('btn-logout').style.display = 'inline-flex';

    // Role-based visibility: Only show Settings button for Admin
    if (this.isAdmin) {
      document.getElementById('btn-settings').style.display = 'inline-flex';
      document.getElementById('badge-admin').style.display = 'inline-flex';
    } else {
      document.getElementById('btn-settings').style.display = 'none';
      document.getElementById('badge-admin').style.display = 'none';
    }

    this.loadHistory();
  }

  showSettings() {
    if (!this.isAdmin) {
      this.showToast('Bạn không có quyền Quản trị viên để truy cập trang Cấu hình!', 'error');
      this.showDashboard();
      return;
    }

    document.getElementById('view-login').classList.remove('active');
    document.getElementById('view-dashboard').classList.remove('active');
    document.getElementById('view-settings').classList.add('active');
    this.populateSettingsForm();
  }

  closeSettings() {
    if (this.uid && this.passcode) {
      this.showDashboard();
    } else {
      this.showLogin();
    }
  }

  // =================== AUTHENTICATION ===================
  async handleLogin(event) {
    event.preventDefault();
    const uid = document.getElementById('login-uid').value.trim();
    const passcode = document.getElementById('login-passcode').value.trim();

    if (!uid || !passcode) {
      this.showToast('Vui lòng nhập đầy đủ UID và Passcode!', 'error');
      return;
    }

    const btnSubmit = document.getElementById('btn-login-submit');
    const originalHtml = btnSubmit.innerHTML;
    btnSubmit.disabled = true;
    btnSubmit.innerHTML = `<span class="spinner"></span> Đang xác thực với RSSP ICorp...`;

    try {
      const resp = await fetch('/api/auth/login', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ uid, passcode })
      });
      const data = await resp.json();

      if (data.success) {
        this.uid = uid;
        this.passcode = passcode;
        this.signerName = data.signerName || '';
        this.isAdmin = data.isAdmin === true;

        localStorage.setItem('eSign_uid', uid);
        localStorage.setItem('eSign_signer_name', this.signerName);
        sessionStorage.setItem('eSign_passcode', passcode);
        sessionStorage.setItem('eSign_is_admin', this.isAdmin ? 'true' : 'false');

        const roleText = this.isAdmin ? ' [Quản Trị Viên]' : '';
        this.showToast(`Đăng nhập thành công! Xin chào, ${this.signerName || uid}${roleText}`, 'success');
        this.showDashboard();
      } else {
        this.showToast(data.message || 'Đăng nhập thất bại!', 'error');
      }
    } catch (err) {
      this.showToast('Lỗi kết nối tới máy chủ: ' + err.message, 'error');
    } finally {
      btnSubmit.disabled = false;
      btnSubmit.innerHTML = originalHtml;
    }
  }

  fillDemoAccount() {
    const accs = this.config?.accounts || [];
    if (accs.length > 0) {
      const firstAcc = accs[0];
      document.getElementById('login-uid').value = firstAcc.agreementUUID || '';
      document.getElementById('login-passcode').value = firstAcc.defaultPasscode || '12345678';
      this.showToast(`Đã điền tài khoản (${firstAcc.signerName || firstAcc.agreementUUID})`, 'info');
    } else {
      this.showToast('Chưa có tài khoản UID nào trong danh bạ.', 'warning');
    }
  }

  fillAdminAccount() {
    document.getElementById('login-uid').value = this.config?.adminUsername || 'admin';
    document.getElementById('login-passcode').value = this.config?.adminPassword || 'admin123';
    this.showToast('Đã điền tài khoản Quản trị viên (admin / admin123)', 'info');
  }

  logout() {
    this.uid = '';
    this.passcode = '';
    this.signerName = '';
    this.isAdmin = false;
    localStorage.removeItem('eSign_uid');
    localStorage.removeItem('eSign_signer_name');
    sessionStorage.removeItem('eSign_passcode');
    sessionStorage.removeItem('eSign_is_admin');
    this.showToast('Đã đăng xuất khỏi hệ thống.', 'info');
    this.showLogin();
  }

  // =================== FILE SELECTION & DRAG-DROP ===================
  setupDropzone() {
    const dropzone = document.getElementById('dropzone');
    if (!dropzone) return;

    ['dragenter', 'dragover'].forEach(eventName => {
      dropzone.addEventListener(eventName, (e) => {
        e.preventDefault();
        dropzone.classList.add('dragover');
      }, false);
    });

    ['dragleave', 'drop'].forEach(eventName => {
      dropzone.addEventListener(eventName, (e) => {
        e.preventDefault();
        dropzone.classList.remove('dragover');
      }, false);
    });

    dropzone.addEventListener('drop', (e) => {
      const files = e.dataTransfer.files;
      if (files && files.length > 0) {
        this.setFile(files[0]);
      }
    });
  }

  handleFileSelect(event) {
    const files = event.target.files;
    if (files && files.length > 0) {
      this.setFile(files[0]);
    }
  }

  setFile(file) {
    const validExts = ['.pdf', '.doc', '.docx'];
    const fileName = file.name.toLowerCase();
    const isValid = validExts.some(ext => fileName.endsWith(ext));

    if (!isValid) {
      this.showToast('Chỉ hỗ trợ file PDF (.pdf) hoặc Word (.doc, .docx)!', 'error');
      return;
    }

    this.selectedFile = file;
    document.getElementById('selected-file-card').style.display = 'flex';
    document.getElementById('selected-file-name').innerText = file.name;
    document.getElementById('selected-file-size').innerText = this.formatFileSize(file.size);

    const iconEl = document.getElementById('selected-file-icon');
    if (fileName.endsWith('.pdf')) {
      iconEl.innerHTML = `<i class="fa-solid fa-file-pdf"></i>`;
      iconEl.style.background = '#ef4444';
    } else {
      iconEl.innerHTML = `<i class="fa-solid fa-file-word"></i>`;
      iconEl.style.background = '#2563eb';
    }
  }

  removeSelectedFile() {
    this.selectedFile = null;
    document.getElementById('selected-file-card').style.display = 'none';
    document.getElementById('file-input').value = '';
  }

  // =================== SIGNING ACTION ===================
  async handleSignFile() {
    if (!this.selectedFile) {
      this.showToast('Vui lòng chọn hoặc kéo thả tài liệu cần ký ở Box 1!', 'warning');
      return;
    }

    const btnSign = document.getElementById('btn-sign-submit');
    const originalHtml = btnSign.innerHTML;
    btnSign.disabled = true;
    btnSign.innerHTML = `<span class="spinner"></span> Đang gửi ký tới RSSP Cloud...`;

    const formData = new FormData();
    formData.append('file', this.selectedFile);
    formData.append('uid', this.uid);
    formData.append('passcode', this.passcode);

    // Custom metadata overrides
    formData.append('ALIGNMENT', document.getElementById('meta-alignment') ? document.getElementById('meta-alignment').value : 'center-below');
    formData.append('PAGENO', document.getElementById('meta-pageno').value.trim());
    formData.append('POSITIONIDENTIFIER', document.getElementById('meta-posid').value.trim());
    formData.append('RECTANGLEOFFSET', document.getElementById('meta-offset').value.trim());
    formData.append('RECTANGLESIZE', document.getElementById('meta-size').value.trim());
    formData.append('SIGNREASON', document.getElementById('meta-reason').value.trim());
    formData.append('LOCATION', document.getElementById('meta-location').value.trim());

    try {
      const resp = await fetch('/api/sign/upload-and-sign', {
        method: 'POST',
        body: formData
      });
      const data = await resp.json();

      if (data.success) {
        this.showToast('🎉 ' + data.message, 'success');
        this.removeSelectedFile();
        await this.loadHistory();

        // Prompt Preview or Download
        if (data.document && data.document.id) {
          this.previewDocument(data.document.id, data.document.signedFileName);
        }
      } else {
        this.showToast('Ký thất bại: ' + (data.message || 'Lỗi không xác định'), 'error', 6000);
      }
    } catch (err) {
      this.showToast('Lỗi gửi ký: ' + err.message, 'error');
    } finally {
      btnSign.disabled = false;
      btnSign.innerHTML = originalHtml;
    }
  }

  // =================== HISTORY MANAGEMENT ===================
  async loadHistory() {
    if (!this.uid) return;

    try {
      const resp = await fetch(`/api/documents/history?uid=${encodeURIComponent(this.uid)}`);
      this.history = await resp.json();
      this.renderHistoryTable(this.history);
    } catch (err) {
      this.showToast('Không thể tải lịch sử tài liệu: ' + err.message, 'error');
    }
  }

  renderHistoryTable(records) {
    const tbody = document.getElementById('history-tbody');
    const emptyEl = document.getElementById('history-empty');
    const badgeEl = document.getElementById('history-count-badge');

    badgeEl.innerText = `${records.length} tài liệu`;

    if (!records || records.length === 0) {
      tbody.innerHTML = '';
      emptyEl.style.display = 'block';
      return;
    }

    emptyEl.style.display = 'none';
    tbody.innerHTML = records.map((doc, idx) => {
      const isPdf = doc.mimeType && doc.mimeType.includes('pdf');
      const extIcon = isPdf ? 'fa-file-pdf' : 'fa-file-word';
      const iconColor = isPdf ? '#f87171' : '#60a5fa';
      const dateStr = new Date(doc.signDate).toLocaleString('vi-VN');

      return `
        <tr>
          <td style="font-family: monospace; color: var(--text-muted);">${idx + 1}</td>
          <td>
            <div style="display: flex; align-items: center; gap: 8px;">
              <i class="fa-solid ${extIcon}" style="color: ${iconColor}; font-size: 1.1rem;"></i>
              <div>
                <strong style="color: #f1f5f9;">${doc.signedFileName || doc.originalFileName}</strong>
                <div style="font-size: 0.75rem; color: var(--text-muted);">${this.formatFileSize(doc.fileSize)} • Lý do: ${doc.reason || 'Ký duyệt'}</div>
              </div>
            </div>
          </td>
          <td>
            <span class="badge-fmt">${isPdf ? 'PDF Signed' : 'Word Signed'}</span>
          </td>
          <td style="font-size: 0.8rem; color: var(--text-secondary);">${dateStr}</td>
          <td style="font-family: monospace; font-size: 0.75rem; color: var(--accent);">
            ${doc.billCode || doc.certificateSerialNumber || 'RSSP-SYNC'}
          </td>
          <td>
            <span class="status-badge success">
              <i class="fa-solid fa-check"></i> Đã ký số
            </span>
          </td>
          <td>
            <div class="actions-cell" style="justify-content: flex-end;">
              ${isPdf ? `
                <button class="btn btn-secondary btn-sm" onclick="app.previewDocument('${doc.id}', '${doc.signedFileName}')" title="Xem trước tài liệu">
                  <i class="fa-solid fa-eye"></i> Xem
                </button>
              ` : ''}
              <a href="/api/documents/download/${doc.id}" class="btn btn-primary btn-sm" title="Tải file đã ký">
                <i class="fa-solid fa-download"></i> Tải
              </a>
              <button class="btn btn-outline btn-sm" onclick="app.showCertificateDetails('${doc.id}')" title="Chi tiết chứng thư số">
                <i class="fa-solid fa-certificate"></i>
              </button>
              <button class="btn btn-danger btn-sm" onclick="app.deleteHistoryItem('${doc.id}')" title="Xóa">
                <i class="fa-solid fa-trash"></i>
              </button>
            </div>
          </td>
        </tr>
      `;
    }).join('');
  }

  filterHistory() {
    const keyword = document.getElementById('history-search').value.toLowerCase().trim();
    if (!keyword) {
      this.renderHistoryTable(this.history);
      return;
    }

    const filtered = this.history.filter(doc =>
      (doc.originalFileName && doc.originalFileName.toLowerCase().includes(keyword)) ||
      (doc.signedFileName && doc.signedFileName.toLowerCase().includes(keyword)) ||
      (doc.billCode && doc.billCode.toLowerCase().includes(keyword)) ||
      (doc.reason && doc.reason.toLowerCase().includes(keyword))
    );
    this.renderHistoryTable(filtered);
  }

  previewDocument(id, fileName) {
    const iframe = document.getElementById('preview-iframe');
    const title = document.getElementById('modal-preview-title');
    const downloadBtn = document.getElementById('modal-preview-download-btn');

    title.innerHTML = `<i class="fa-regular fa-file-pdf"></i> Xem Trước: ${fileName || 'Tài liệu đã ký'}`;
    iframe.src = `/api/documents/preview/${id}`;
    downloadBtn.href = `/api/documents/download/${id}`;

    this.openModal('modal-preview');
  }

  showCertificateDetails(id) {
    const doc = this.history.find(d => d.id === id);
    if (!doc) return;

    const body = document.getElementById('modal-cert-body');
    body.innerHTML = `
      <div style="background: rgba(15,23,42,0.6); padding: 16px; border-radius: var(--radius-md); border: 1px solid var(--border-color);">
        <div style="margin-bottom: 12px;">
          <div style="font-size: 0.75rem; color: var(--text-muted);">Tài liệu:</div>
          <strong>${doc.signedFileName}</strong>
        </div>
        <div style="margin-bottom: 12px;">
          <div style="font-size: 0.75rem; color: var(--text-muted);">Mã hóa đơn / Bill Code:</div>
          <code style="color: var(--accent); font-family: monospace;">${doc.billCode || 'N/A'}</code>
        </div>
        <div style="margin-bottom: 12px;">
          <div style="font-size: 0.75rem; color: var(--text-muted);">Certificate DN:</div>
          <div style="font-size: 0.85rem; word-break: break-all;">${doc.certificateDN || 'Tài khoản RSSP Demo CA'}</div>
        </div>
        <div style="margin-bottom: 12px;">
          <div style="font-size: 0.75rem; color: var(--text-muted);">Serial Number:</div>
          <code style="font-family: monospace;">${doc.certificateSerialNumber || 'N/A'}</code>
        </div>
        <div style="margin-bottom: 12px;">
          <div style="font-size: 0.75rem; color: var(--text-muted);">Đơn vị phát hành (Issuer DN):</div>
          <div style="font-size: 0.85rem;">${doc.issuerDN || 'C=VN,O=I-CA,CN=I-CA SHA-256'}</div>
        </div>
        <div>
          <div style="font-size: 0.75rem; color: var(--text-muted);">Thời gian ký:</div>
          <div>${new Date(doc.signDate).toLocaleString('vi-VN')}</div>
        </div>
      </div>
    `;
    this.openModal('modal-cert');
  }

  async deleteHistoryItem(id) {
    if (!confirm('Bạn có chắc chắn muốn xóa bản ghi ký này khỏi danh sách?')) return;

    try {
      const resp = await fetch(`/api/documents/${id}`, { method: 'DELETE' });
      const data = await resp.json();
      if (data.success) {
        this.showToast('Đã xóa tài liệu khỏi danh sách.', 'info');
        await this.loadHistory();
      }
    } catch (err) {
      this.showToast('Không thể xóa: ' + err.message, 'error');
    }
  }

  // =================== CONFIGURATION ===================
  async loadConfig() {
    try {
      const resp = await fetch('/api/config');
      this.config = await resp.json();

      // Update Sliders
      if (this.config && this.config.slides) {
        sliderLogin.setSlides(this.config.slides);
        sliderDashboard.setSlides(this.config.slides);
      }

      // Update Quick Defaults for metadata
      if (this.config && this.config.defaultMetadata) {
        const m = this.config.defaultMetadata;
        if (m.ALIGNMENT && document.getElementById('meta-alignment')) document.getElementById('meta-alignment').value = m.ALIGNMENT;
        if (m.PAGENO) document.getElementById('meta-pageno').value = m.PAGENO;
        if (m.POSITIONIDENTIFIER) document.getElementById('meta-posid').value = m.POSITIONIDENTIFIER;
        if (m.RECTANGLEOFFSET) document.getElementById('meta-offset').value = m.RECTANGLEOFFSET;
        if (m.RECTANGLESIZE) document.getElementById('meta-size').value = m.RECTANGLESIZE;
        if (m.SIGNREASON) document.getElementById('meta-reason').value = m.SIGNREASON;
        if (m.LOCATION) document.getElementById('meta-location').value = m.LOCATION;
      }
    } catch (err) {
      console.error('Failed to load config:', err);
    }
  }

  populateSettingsForm() {
    if (!this.config) return;
    const c = this.config;
    const m = c.defaultMetadata || {};

    // Tab 1
    document.getElementById('cfg-rest-url').value = c.restUrl || '';
    document.getElementById('cfg-rp').value = c.relyingParty || '';
    document.getElementById('cfg-rp-user').value = c.relyingPartyUser || '';
    document.getElementById('cfg-rp-pwd').value = c.relyingPartyPassword || '';
    document.getElementById('cfg-rp-sig').value = c.relyingPartySignature || '';
    document.getElementById('cfg-rp-keystore').value = c.relyingPartyKeyStore || '';
    document.getElementById('cfg-rp-keystore-pwd').value = c.relyingPartyKeyStorePassword || '';
    document.getElementById('cfg-cert-profile').value = c.certificateProfile || '';
    document.getElementById('cfg-file-dir').value = c.fileDirectory || '';
    document.getElementById('cfg-default-uid').value = c.defaultAgreementUUID || '';
    document.getElementById('cfg-default-passcode').value = c.defaultPassCode || '';

    // Tab 2
    if (document.getElementById('cfg-meta-alignment')) document.getElementById('cfg-meta-alignment').value = m.ALIGNMENT || 'center-below';
    document.getElementById('cfg-meta-pageno').value = m.PAGENO || '1';
    document.getElementById('cfg-meta-posid').value = m.POSITIONIDENTIFIER || '(Ký tên, đóng dấu)';
    document.getElementById('cfg-meta-offset').value = m.RECTANGLEOFFSET || '0,0';
    document.getElementById('cfg-meta-size').value = m.RECTANGLESIZE || '170,70';
    document.getElementById('cfg-meta-visible').value = m.VISIBLESIGNATURE || 'True';
    document.getElementById('cfg-meta-color').value = m.TEXTCOLOR || 'black';
    document.getElementById('cfg-meta-direction').value = m.TEXTDIRECTION || 'LEFTTORIGHT';
    document.getElementById('cfg-meta-signer-prefix').value = m.SIGNERINFOPREFIX || 'Ký bởi:';
    document.getElementById('cfg-meta-date-prefix').value = m.DATETIMEPREFIX || 'Ký ngày:';
    document.getElementById('cfg-meta-reason').value = m.SIGNREASON || '';
    document.getElementById('cfg-meta-location').value = m.LOCATION || '';

    // Tab 3
    this.renderSlidesEditor(c.slides || []);

    // Tab 4
    this.renderUidsTable();
  }

  renderUidsTable() {
    const tbody = document.getElementById('uids-tbody');
    if (!tbody) return;
    const accounts = this.config?.accounts || [];

    if (accounts.length === 0) {
      tbody.innerHTML = `<tr><td colspan="8" style="text-align: center; color: var(--text-muted); padding: 24px;">Chưa có tài khoản UID nào. Hãy bấm "Thêm UID Mới".</td></tr>`;
      return;
    }

    tbody.innerHTML = accounts.map((acc, idx) => `
      <tr>
        <td style="font-family: monospace; color: var(--text-muted);">${idx + 1}</td>
        <td>
          <div style="display: flex; align-items: center; gap: 8px;">
            <div class="user-avatar" style="width: 26px; height: 26px; font-size: 11px;">${(acc.signerName || 'U').charAt(0).toUpperCase()}</div>
            <strong>${acc.signerName || 'Chưa đặt tên'}</strong>
          </div>
        </td>
        <td><code style="color: var(--accent); font-size: 0.8rem;">${acc.agreementUUID}</code></td>
        <td>
          ${acc.phone ? `<span style="color: #38bdf8; font-family: monospace; font-size: 0.82rem; display: inline-flex; align-items: center; gap: 4px;"><i class="fa-solid fa-phone" style="font-size: 0.72rem; opacity: 0.75;"></i>${acc.phone}</span>` : '<span style="color: var(--text-muted); font-size: 0.8rem;">--</span>'}
        </td>
        <td style="text-align: center;">
          <span class="badge-fmt" style="background: rgba(56, 189, 248, 0.12); color: #38bdf8; border: 1px solid rgba(56, 189, 248, 0.25); padding: 3px 9px; border-radius: 12px; font-weight: 600; font-size: 0.78rem; display: inline-flex; align-items: center; gap: 4px;">
            <i class="fa-solid fa-file-circle-check"></i> ${acc.signedCount || 0}
          </span>
        </td>
        <td><span style="font-family: monospace; color: #cbd5e1;">••••••••</span></td>
        <td><span class="status-badge success"><i class="fa-solid fa-circle-check"></i> ${acc.status || 'Hoạt động'}</span></td>
        <td>
          <div class="actions-cell" style="justify-content: flex-end;">
            <button type="button" class="btn btn-secondary btn-sm" onclick="app.verifyUidCertificate('${acc.agreementUUID}', '${acc.defaultPasscode}')" title="Kiểm tra chứng thư RSSP">
              <i class="fa-solid fa-certificate"></i>
            </button>
            <button type="button" class="btn btn-primary btn-sm" onclick="app.switchToUid('${acc.agreementUUID}', '${acc.defaultPasscode}', '${acc.signerName}')" title="Đăng nhập & Ký bằng UID này">
              <i class="fa-solid fa-arrow-right-to-bracket"></i> Ký
            </button>
            <button type="button" class="btn btn-danger btn-sm" onclick="app.deleteUid('${acc.agreementUUID}')" title="Xóa">
              <i class="fa-solid fa-trash"></i>
            </button>
          </div>
        </td>
      </tr>
    `).join('');
  }

  openAddUidModal() {
    document.getElementById('uid-input-uuid').value = '';
    document.getElementById('uid-input-name').value = '';
    document.getElementById('uid-input-dept').value = '';
    document.getElementById('uid-input-email').value = '';
    document.getElementById('uid-input-phone').value = '';
    document.getElementById('uid-input-passcode').value = '12345678';
    document.getElementById('uid-input-status').value = 'Hoạt động';
    this.openModal('modal-add-uid');
  }

  async handleSaveUid(event) {
    event.preventDefault();
    const account = {
      agreementUUID: document.getElementById('uid-input-uuid').value.trim(),
      signerName: document.getElementById('uid-input-name').value.trim(),
      department: document.getElementById('uid-input-dept').value.trim(),
      email: document.getElementById('uid-input-email').value.trim(),
      phone: document.getElementById('uid-input-phone').value.trim(),
      defaultPasscode: document.getElementById('uid-input-passcode').value.trim(),
      status: document.getElementById('uid-input-status').value
    };

    try {
      const resp = await fetch('/api/admin/uids', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(account)
      });
      const data = await resp.json();
      if (data.success) {
        this.config.accounts = data.accounts;
        this.renderUidsTable();
        this.showToast('Đã lưu tài khoản UID thành công!', 'success');
        this.closeModal('modal-add-uid');
      } else {
        this.showToast(data.message || 'Lỗi lưu UID', 'error');
      }
    } catch (err) {
      this.showToast('Lỗi: ' + err.message, 'error');
    }
  }

  async deleteUid(uuid) {
    if (!confirm(`Bạn có chắc muốn xóa UID: ${uuid} khỏi danh bạ?`)) return;
    try {
      const resp = await fetch(`/api/admin/uids/${encodeURIComponent(uuid)}`, { method: 'DELETE' });
      const data = await resp.json();
      if (data.success) {
        this.config.accounts = data.accounts;
        if (this.uid && this.uid.toLowerCase() === uuid.toLowerCase()) {
          this.uid = '';
          localStorage.removeItem('eSign_uid');
        }
        this.renderUidsTable();
        this.showToast('Đã xóa UID thành công!', 'info');
      }
    } catch (err) {
      this.showToast('Lỗi: ' + err.message, 'error');
    }
  }

  async verifyUidCertificate(uuid, passcode) {
    this.showToast('Đang gọi RSSP getCertificateDetailForSignCloud...', 'info');
    try {
      const resp = await fetch('/api/admin/uids/verify', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ uid: uuid, passcode: passcode || '12345678' })
      });
      const data = await resp.json();
      if (data.success) {
        const body = document.getElementById('modal-cert-body');
        body.innerHTML = `
          <div style="background: rgba(15,23,42,0.6); padding: 16px; border-radius: var(--radius-md); border: 1px solid var(--border-color);">
            <div style="margin-bottom: 10px;"><span style="color: var(--text-muted);">UID:</span> <code>${uuid}</code></div>
            <div style="margin-bottom: 10px;"><span style="color: var(--text-muted);">Tên chủ chứng thư:</span> <strong>${data.signerName || 'N/A'}</strong></div>
            <div style="margin-bottom: 10px;"><span style="color: var(--text-muted);">Certificate DN:</span> <div style="font-size: 0.85rem;">${data.certificateDN || 'N/A'}</div></div>
            <div style="margin-bottom: 10px;"><span style="color: var(--text-muted);">Serial Number:</span> <code style="color: var(--accent);">${data.serialNumber || 'N/A'}</code></div>
            <div><span class="status-badge success"><i class="fa-solid fa-check"></i> Chứng thư hợp lệ trên RSSP Cloud</span></div>
          </div>
        `;
        this.openModal('modal-cert');
      } else {
        this.showToast('Chứng thư không hợp lệ: ' + data.message, 'error');
      }
    } catch (err) {
      this.showToast('Lỗi: ' + err.message, 'error');
    }
  }

  switchToUid(uuid, passcode, name) {
    this.uid = uuid;
    this.passcode = passcode || '12345678';
    this.signerName = name || '';
    localStorage.setItem('eSign_uid', this.uid);
    localStorage.setItem('eSign_signer_name', this.signerName);
    sessionStorage.setItem('eSign_passcode', this.passcode);
    this.showToast(`Đã chuyển sang tài khoản: ${this.signerName || this.uid}`, 'success');
    this.showDashboard();
  }

  renderSlidesEditor(slides) {
    const container = document.getElementById('slides-editor-container');
    if (!container) return;

    const slidesList = slides || [];
    container.innerHTML = slidesList.map((s, idx) => {
      const hasImg = s.imageUrl && s.imageUrl.trim() !== '';
      return `
      <div class="slide-edit-card" data-idx="${idx}" style="background: rgba(15,23,42,0.6); padding: 18px; border-radius: var(--radius-md); border: 1px solid var(--border-color); margin-bottom: 18px; position: relative;">
        <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 14px; border-bottom: 1px solid rgba(255,255,255,0.08); padding-bottom: 8px;">
          <div style="display: flex; align-items: center; gap: 8px;">
            <strong style="color: #93c5fd;"><i class="fa-solid fa-layer-group"></i> Slide #${idx + 1}</strong>
            <span class="slide-tag" style="margin: 0;">${s.tag || 'BANNER'}</span>
          </div>
          ${slidesList.length > 1 ? `
            <button type="button" class="btn btn-outline btn-sm" style="color: #f87171; border-color: rgba(248,113,113,0.3); padding: 3px 8px; font-size: 0.75rem;" onclick="app.deleteSlide(${idx})" title="Xóa slide này">
              <i class="fa-solid fa-trash-can"></i> Xóa slide
            </button>
          ` : ''}
        </div>

        <div class="form-grid-2">
          <div class="form-group" style="margin-bottom: 10px;">
            <label class="form-label" style="font-size: 0.75rem;">Nhãn (Tag):</label>
            <input type="text" class="form-input slide-edit-tag" value="${s.tag || ''}" data-idx="${idx}" placeholder="Ví dụ: TÍNH NĂNG NỔI BẬT">
          </div>
          <div class="form-group" style="margin-bottom: 10px;">
            <label class="form-label" style="font-size: 0.75rem;">Tiêu đề slide:</label>
            <input type="text" class="form-input slide-edit-title" value="${s.title || ''}" data-idx="${idx}" placeholder="Tiêu đề banner quảng cáo">
          </div>
        </div>

        <div class="form-group" style="margin-bottom: 10px;">
          <label class="form-label" style="font-size: 0.75rem;">
            <span><i class="fa-solid fa-link" style="color: var(--accent);"></i> <b>Backlink / URL liên kết Tiêu đề</b>:</span>
            <span style="font-size: 0.7rem; color: var(--text-muted); font-weight: normal;">(Khi click vào tiêu đề slide sẽ mở link này)</span>
          </label>
          <input type="text" class="form-input slide-edit-link" value="${s.linkUrl || s.buttonLink || ''}" data-idx="${idx}" placeholder="Ví dụ: https://pmbk.vn hoặc https://i-ca.vn">
        </div>

        <div class="form-group" style="margin-bottom: 12px;">
          <label class="form-label" style="font-size: 0.75rem;">Mô tả chi tiết:</label>
          <textarea class="form-input slide-edit-desc" data-idx="${idx}" rows="2" style="resize: vertical;" placeholder="Nội dung mô tả giới thiệu">${s.description || ''}</textarea>
        </div>

        <!-- Background Options: Image Upload vs CSS Gradient -->
        <div style="background: rgba(0,0,0,0.25); border: 1px solid rgba(255,255,255,0.06); border-radius: var(--radius-sm); padding: 12px; margin-bottom: 10px;">
          <label class="form-label" style="font-size: 0.8rem; margin-bottom: 8px; display: flex; justify-content: space-between; align-items: center;">
            <span><i class="fa-solid fa-image" style="color: var(--accent);"></i> <b>Ảnh Nền Banner (Thay thế Gradient)</b>:</span>
            <span style="font-size: 0.72rem; color: #94a3b8; font-weight: normal;">Ưu tiên hiển thị nếu có</span>
          </label>

          <div class="slide-edit-bg-row">
            <!-- Thumbnail Preview -->
            <div id="slide-preview-box-${idx}" style="width: 140px; height: 80px; border-radius: 6px; border: 1px dashed rgba(255,255,255,0.2); overflow: hidden; display: flex; align-items: center; justify-content: center; background: ${hasImg ? `#0f172a url('${s.imageUrl}') center/${s.imageFit || 'contain'} no-repeat` : (s.gradient || '#1e293b')}; flex-shrink: 0; position: relative;">
              ${!hasImg ? `<span style="font-size: 0.7rem; color: #94a3b8; text-align: center; padding: 4px;"><i class="fa-solid fa-palette"></i><br>Đang dùng Gradient</span>` : ''}
            </div>

            <!-- Upload Controls -->
            <div style="flex: 1;">
              <input type="hidden" class="slide-edit-image" id="slide-img-val-${idx}" value="${s.imageUrl || ''}" data-idx="${idx}">
              <input type="file" id="slide-file-input-${idx}" accept="image/*" style="display: none;" onchange="app.handleSlideImageUpload(event, ${idx})">

              <div style="display: flex; gap: 8px; flex-wrap: wrap; margin-bottom: 8px;">
                <button type="button" class="btn btn-primary btn-sm" onclick="document.getElementById('slide-file-input-${idx}').click()">
                  <i class="fa-solid fa-arrow-up-from-bracket"></i> ${hasImg ? 'Thay ảnh khác' : 'Tải ảnh banner lên'}
                </button>
                ${hasImg ? `
                  <button type="button" class="btn btn-outline btn-sm" style="color: #f87171;" onclick="app.removeSlideImage(${idx})">
                    <i class="fa-solid fa-trash-can"></i> Xóa ảnh (dùng Gradient)
                  </button>
                ` : ''}
              </div>

              <div class="form-grid-2" style="margin-bottom: 0;">
                <div class="form-group" style="margin-bottom: 0;">
                  <label class="form-label" style="font-size: 0.7rem; color: var(--text-muted);">Kiểu vừa khung ảnh (Fit Mode):</label>
                  <select class="form-input slide-edit-fit" data-idx="${idx}" onchange="app.updateSlideImageFit(${idx}, this.value)">
                    <option value="contain" ${(s.imageFit || 'contain') === 'contain' ? 'selected' : ''}>Vừa vặn khung (Contain - Thấy trọn vẹn 100% ảnh)</option>
                    <option value="100% 100%" ${s.imageFit === '100% 100%' ? 'selected' : ''}>Kéo vừa khít khung (100% 100%)</option>
                    <option value="cover" ${s.imageFit === 'cover' ? 'selected' : ''}>Phủ đầy khung (Cover - Phóng to cắt mép)</option>
                  </select>
                </div>
                <div class="form-group" style="margin-bottom: 0;">
                  <label class="form-label" style="font-size: 0.7rem; color: var(--text-muted);">Đường dẫn / URL ảnh trực tiếp:</label>
                  <input type="text" class="form-input slide-edit-image-url" id="slide-img-url-input-${idx}" value="${s.imageUrl || ''}" data-idx="${idx}" placeholder="/assets/banners/my_image.png..." oninput="app.updateSlideImagePreview(${idx}, this.value)">
                </div>
              </div>
            </div>
          </div>
        </div>

        <div class="form-group" style="margin-bottom: 0;">
          <label class="form-label" style="font-size: 0.75rem;">
            <span><i class="fa-solid fa-palette"></i> Gradient nền CSS (Dự phòng / Khi không dùng ảnh):</span>
          </label>
          <input type="text" class="form-input slide-edit-gradient" value="${s.gradient || ''}" data-idx="${idx}" placeholder="linear-gradient(135deg, #1e3c72 0%, #2a5298 100%)">
        </div>
      </div>
      `;
    }).join('');
  }

  async handleSlideImageUpload(event, idx) {
    const input = event.target;
    const file = input.files && input.files[0];
    if (!file) return;

    const formData = new FormData();
    formData.append('file', file);

    this.showToast('Đang tải ảnh banner lên máy chủ...', 'info');

    try {
      const resp = await fetch('/api/admin/slides/upload-image', {
        method: 'POST',
        body: formData
      });

      let data;
      try {
        data = await resp.json();
      } catch {
        const txt = await resp.text();
        throw new Error(`Máy chủ phản hồi (HTTP ${resp.status}): ${txt}`);
      }

      if (resp.ok && data.success && data.imageUrl) {
        this.showToast('Tải ảnh banner thành công!', 'success');

        // Update model
        if (this.config.slides && this.config.slides[idx]) {
          this.config.slides[idx].imageUrl = data.imageUrl;
        }

        // Re-render the editor with new preview and controls
        this.renderSlidesEditor(this.config.slides);

        // Immediately persist configuration
        await this.saveConfig();
      } else {
        this.showToast(data.message || 'Lỗi tải ảnh banner', 'error');
      }
    } catch (err) {
      this.showToast('Lỗi tải ảnh: ' + err.message, 'error');
    } finally {
      input.value = '';
    }
  }

  async removeSlideImage(idx) {
    if (this.config.slides && this.config.slides[idx]) {
      this.config.slides[idx].imageUrl = '';
    }
    await this.saveConfig();
    this.renderSlidesEditor(this.config.slides);
    this.showToast('Đã xóa ảnh banner, chuyển về dùng Gradient nền!', 'info');
  }

  updateSlideImagePreview(idx, val) {
    const hiddenInput = document.getElementById(`slide-img-val-${idx}`);
    if (hiddenInput) hiddenInput.value = val;

    const fitSelect = document.querySelector(`.slide-edit-fit[data-idx="${idx}"]`);
    const fitMode = fitSelect ? fitSelect.value : 'contain';

    const previewBox = document.getElementById(`slide-preview-box-${idx}`);
    if (previewBox) {
      if (val && val.trim() !== '') {
        previewBox.style.background = `#0f172a url('${val.trim()}') center/${fitMode} no-repeat`;
        previewBox.innerHTML = '';
      } else {
        const gradInput = document.querySelector(`.slide-edit-gradient[data-idx="${idx}"]`);
        const gradVal = gradInput ? gradInput.value : '#1e293b';
        previewBox.style.background = gradVal;
        previewBox.innerHTML = '<span style="font-size: 0.7rem; color: #94a3b8; text-align: center; padding: 4px;"><i class="fa-solid fa-palette"></i><br>Đang dùng Gradient</span>';
      }
    }
  }

  async updateSlideImageFit(idx, fit) {
    if (this.config.slides && this.config.slides[idx]) {
      this.config.slides[idx].imageFit = fit;
    }
    const hiddenInput = document.getElementById(`slide-img-val-${idx}`);
    const imgUrl = (hiddenInput ? hiddenInput.value : (this.config.slides[idx]?.imageUrl || '')).trim();

    const previewBox = document.getElementById(`slide-preview-box-${idx}`);
    if (previewBox && imgUrl !== '') {
      previewBox.style.background = `#0f172a url('${imgUrl}') center/${fit} no-repeat`;
    }
    await this.saveConfig();
  }

  handleAlignmentChange(alignVal, targetOffsetId) {
    const el = document.getElementById(targetOffsetId);
    if (!el) return;
    if (el.value === '0,-10' || el.value === '-30,-100' || el.value === '') {
      el.value = '0,0';
    }
  }

  async addSlide() {
    this.config.slides = this.config.slides || [];
    const newId = this.config.slides.length + 1;
    this.config.slides.push({
      Id: newId,
      Tag: 'TÍNH NĂNG MỚI',
      Title: 'Giải Pháp Ký Số Đám Mây Toàn Diện',
      Description: 'Tích hợp chữ ký số bảo mật cao theo tiêu chuẩn Mobile-ID RSSP.',
      Gradient: 'linear-gradient(135deg, #1e3c72 0%, #2a5298 100%)',
      ImageUrl: '',
      Icon: 'star',
      ButtonText: 'Khám phá ngay',
      ButtonLink: '#'
    });

    this.renderSlidesEditor(this.config.slides);
    await this.saveConfig();
    this.showToast('Đã thêm Slide mới!', 'success');
  }

  async deleteSlide(idx) {
    if (!this.config.slides || this.config.slides.length <= 1) {
      this.showToast('Phải giữ lại ít nhất 1 Slide!', 'warning');
      return;
    }

    if (!confirm(`Bạn có chắc chắn muốn xóa Slide #${idx + 1}?`)) return;

    this.config.slides.splice(idx, 1);
    this.renderSlidesEditor(this.config.slides);
    await this.saveConfig();
    this.showToast('Đã xóa Slide thành công!', 'success');
  }

  async saveConfig() {
    const reasonVal = document.getElementById('cfg-meta-reason').value.trim();
    const locVal = document.getElementById('cfg-meta-location').value.trim();

    const updated = {
      restUrl: document.getElementById('cfg-rest-url').value.trim(),
      relyingParty: document.getElementById('cfg-rp').value.trim(),
      relyingPartyUser: document.getElementById('cfg-rp-user').value.trim(),
      relyingPartyPassword: document.getElementById('cfg-rp-pwd').value.trim(),
      relyingPartySignature: document.getElementById('cfg-rp-sig').value.trim(),
      relyingPartyKeyStore: document.getElementById('cfg-rp-keystore').value.trim(),
      relyingPartyKeyStorePassword: document.getElementById('cfg-rp-keystore-pwd').value.trim(),
      certificateProfile: document.getElementById('cfg-cert-profile').value.trim(),
      fileDirectory: document.getElementById('cfg-file-dir').value.trim(),
      defaultAgreementUUID: document.getElementById('cfg-default-uid').value.trim(),
      defaultPassCode: document.getElementById('cfg-default-passcode').value.trim(),
      defaultMetadata: {
        ALIGNMENT: document.getElementById('cfg-meta-alignment') ? document.getElementById('cfg-meta-alignment').value : 'center-below',
        PAGENO: document.getElementById('cfg-meta-pageno').value.trim(),
        POSITIONIDENTIFIER: document.getElementById('cfg-meta-posid').value.trim(),
        RECTANGLEOFFSET: document.getElementById('cfg-meta-offset').value.trim(),
        RECTANGLESIZE: document.getElementById('cfg-meta-size').value.trim(),
        VISIBLESIGNATURE: document.getElementById('cfg-meta-visible').value,
        VISUALSTATUS: "False",
        SHOWSIGNERINFO: "True",
        SIGNERINFOPREFIX: document.getElementById('cfg-meta-signer-prefix').value.trim(),
        SHOWDATETIME: "True",
        DATETIMEPREFIX: document.getElementById('cfg-meta-date-prefix').value.trim(),
        SHOWREASON: reasonVal !== '' ? "True" : "False",
        SIGNREASONPREFIX: "Lý do:",
        SIGNREASON: reasonVal,
        SHOWLOCATION: locVal !== '' ? "True" : "False",
        LOCATION: locVal,
        LOCATIONPREFIX: "Nơi ký:",
        TEXTCOLOR: document.getElementById('cfg-meta-color').value,
        IMAGEANDTEXT: "False",
        TEXTDIRECTION: document.getElementById('cfg-meta-direction').value
      },
      adminUsername: this.config?.adminUsername || 'admin',
      adminPassword: this.config?.adminPassword || 'admin123',
      adminUids: this.config?.adminUids || [],
      enableDemoSimulation: this.config?.enableDemoSimulation !== undefined ? this.config.enableDemoSimulation : true,
      accounts: this.config?.accounts || [],
      slides: (this.config.slides || []).map((s, idx) => {
        const tagEl = document.querySelector(`.slide-edit-tag[data-idx="${idx}"]`);
        const titleEl = document.querySelector(`.slide-edit-title[data-idx="${idx}"]`);
        const linkEl = document.querySelector(`.slide-edit-link[data-idx="${idx}"]`);
        const descEl = document.querySelector(`.slide-edit-desc[data-idx="${idx}"]`);
        const gradEl = document.querySelector(`.slide-edit-gradient[data-idx="${idx}"]`);
        const imgValEl = document.getElementById(`slide-img-val-${idx}`);
        const imgUrlInputEl = document.getElementById(`slide-img-url-input-${idx}`);

        let imgUrl = (s.imageUrl || s.ImageUrl || '').trim();
        if (imgValEl && imgValEl.value !== undefined && imgValEl.value !== '') imgUrl = imgValEl.value.trim();
        else if (imgUrlInputEl && imgUrlInputEl.value !== undefined && imgUrlInputEl.value !== '') imgUrl = imgUrlInputEl.value.trim();

        let linkVal = (s.linkUrl || s.LinkUrl || s.buttonLink || s.ButtonLink || '').trim();
        if (linkEl && linkEl.value !== undefined) linkVal = linkEl.value.trim();

        const fitEl = document.querySelector(`.slide-edit-fit[data-idx="${idx}"]`);
        const fitVal = fitEl ? fitEl.value : (s.imageFit || s.ImageFit || 'contain');

        return {
          id: s.id || s.Id || (idx + 1),
          tag: tagEl ? tagEl.value.trim() : (s.tag || s.Tag || 'BANNER'),
          title: titleEl ? titleEl.value.trim() : (s.title || s.Title || ''),
          description: descEl ? descEl.value.trim() : (s.description || s.Description || ''),
          gradient: gradEl ? gradEl.value.trim() : (s.gradient || s.Gradient || 'linear-gradient(135deg, #1e3c72 0%, #2a5298 100%)'),
          imageUrl: imgUrl,
          imageFit: fitVal,
          linkUrl: linkVal,
          buttonLink: linkVal,
          icon: s.icon || s.Icon || 'shield-check',
          buttonText: s.buttonText || s.ButtonText || 'Khám phá ngay'
        };
      })
    };

    try {
      const resp = await fetch('/api/config', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(updated)
      });
      const data = await resp.json();
      if (data.success) {
        this.config = data.config;
        this.showToast('Đã lưu cấu hình thành công!', 'success');
        if (this.config.slides) {
          sliderLogin.setSlides(this.config.slides);
          sliderDashboard.setSlides(this.config.slides);
        }
        this.renderUidsTable();
      }
    } catch (err) {
      this.showToast('Lỗi lưu cấu hình: ' + err.message, 'error');
    }
  }

  async resetConfig() {
    if (!confirm('Bạn có muốn khôi phục cấu hình về mặc định ban đầu?')) return;
    try {
      const resp = await fetch('/api/config/reset', { method: 'POST' });
      const data = await resp.json();
      if (data.success) {
        this.config = data.config;
        this.populateSettingsForm();
        this.showToast('Đã khôi phục cấu hình mặc định!', 'info');
      }
    } catch (err) {
      this.showToast('Lỗi: ' + err.message, 'error');
    }
  }

  async testConnection() {
    try {
      const resp = await fetch('/api/config/test-connection', { method: 'POST' });
      const data = await resp.json();
      if (data.success) {
        this.showToast('✓ ' + data.message, 'success', 5000);
      } else {
        this.showToast('✗ ' + data.message, 'error', 6000);
      }
    } catch (err) {
      this.showToast('Lỗi kiểm tra kết nối: ' + err.message, 'error');
    }
  }

  async uploadKeystoreFile(event) {
    const file = event.target.files[0];
    if (!file) return;

    const formData = new FormData();
    formData.append('keystoreFile', file);

    try {
      const resp = await fetch('/api/config/upload-keystore', {
        method: 'POST',
        body: formData
      });
      const data = await resp.json();
      if (data.success) {
        document.getElementById('cfg-rp-keystore').value = data.filePath;
        this.showToast(data.message, 'success');
      } else {
        this.showToast(data.message, 'error');
      }
    } catch (err) {
      this.showToast('Lỗi tải file keystore: ' + err.message, 'error');
    }
  }

  switchSettingsTab(tabId, btnEl) {
    document.querySelectorAll('.settings-tab-btn').forEach(b => b.classList.remove('active'));
    document.querySelectorAll('.settings-tab-content').forEach(c => c.classList.remove('active'));

    btnEl.classList.add('active');
    document.getElementById(tabId).classList.add('active');
  }

  // =================== PASSCODE OPERATIONS ===================
  async handleChangePasscode(event) {
    event.preventDefault();
    const uid = document.getElementById('change-uid').value.trim();
    const currentPasscode = document.getElementById('change-old-pwd').value.trim();
    const newPasscode = document.getElementById('change-new-pwd').value.trim();

    try {
      const resp = await fetch('/api/auth/change-passcode', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ uid, currentPasscode, newPasscode })
      });
      const data = await resp.json();
      if (data.success) {
        this.showToast(data.message, 'success');
        this.closeModal('modal-change-passcode');
      } else {
        this.showToast(data.message, 'error');
      }
    } catch (err) {
      this.showToast('Lỗi: ' + err.message, 'error');
    }
  }

  async handleForgetPasscode(event) {
    event.preventDefault();
    const uid = document.getElementById('forget-uid').value.trim();

    try {
      const resp = await fetch('/api/auth/forget-passcode', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ uid })
      });
      const data = await resp.json();
      if (data.success) {
        this.showToast(data.message, 'success');
        this.closeModal('modal-forget-passcode');
      } else {
        this.showToast(data.message, 'error');
      }
    } catch (err) {
      this.showToast('Lỗi: ' + err.message, 'error');
    }
  }

  // =================== HELPERS ===================
  toggleAccordion(id) {
    const el = document.getElementById(id);
    if (el) el.classList.toggle('open');
  }

  togglePasswordVisibility(inputId, btnEl) {
    const input = document.getElementById(inputId);
    if (input.type === 'password') {
      input.type = 'text';
      btnEl.innerHTML = `<i class="fa-solid fa-eye-slash"></i>`;
    } else {
      input.type = 'password';
      btnEl.innerHTML = `<i class="fa-solid fa-eye"></i>`;
    }
  }

  openModal(modalId) {
    const modal = document.getElementById(modalId);
    if (modal) modal.classList.add('active');
  }

  closeModal(modalId) {
    const modal = document.getElementById(modalId);
    if (modal) modal.classList.remove('active');
    if (modalId === 'modal-preview') {
      document.getElementById('preview-iframe').src = 'about:blank';
    }
  }

  formatFileSize(bytes) {
    if (!bytes || bytes === 0) return '0 B';
    const k = 1024;
    const sizes = ['B', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(1)) + ' ' + sizes[i];
  }

  showToast(message, type = 'info', duration = 4000) {
    const container = document.getElementById('toast-container');
    if (!container) return;

    const toast = document.createElement('div');
    toast.className = `toast ${type}`;
    const icon = type === 'success' ? 'fa-circle-check' : (type === 'error' ? 'fa-circle-xmark' : 'fa-circle-info');
    toast.innerHTML = `<i class="fa-solid ${icon}"></i><span>${message}</span>`;

    container.appendChild(toast);
    setTimeout(() => {
      toast.style.opacity = '0';
      toast.style.transform = 'translateX(100%)';
      toast.style.transition = 'all 0.3s ease';
      setTimeout(() => toast.remove(), 300);
    }, duration);
  }
}

const app = new AppController();
document.addEventListener('DOMContentLoaded', () => app.init());
