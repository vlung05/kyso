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
        <div style="margin-bottom: 12px;">
          <div style="font-size: 0.75rem; color: var(--text-muted);">Thời hạn hiệu lực chứng thư:</div>
          <div style="font-size: 0.85rem; display: flex; flex-direction: column; gap: 4px; margin-top: 4px;">
            <div style="color: #38bdf8;"><i class="fa-regular fa-calendar-check" style="margin-right: 6px;"></i><strong>Hiệu lực từ:</strong> ${doc.validFrom ? new Date(doc.validFrom).toLocaleString('vi-VN') : 'N/A'}</div>
            <div style="color: #fbbf24;"><i class="fa-regular fa-calendar-xmark" style="margin-right: 6px;"></i><strong>Hiệu lực đến:</strong> ${doc.validTo ? new Date(doc.validTo).toLocaleString('vi-VN') : 'N/A'}</div>
          </div>
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

  extractTaxId(acc) {
    if (!acc) return '';
    if (acc.taxId && acc.taxId.trim() !== '') return acc.taxId.trim();
    if (acc.taxCode && acc.taxCode.trim() !== '') return acc.taxCode.trim();
    return this.extractTaxIdFromDn(acc.certificateDN) || '';
  }

  extractTaxIdFromDn(dn) {
    if (!dn) return '';
    // 1. UID=MST:038079004321 or UID=CCCD:... or UID=CMND:... or UID=038079004321
    let m = dn.match(/UID\s*=\s*(?:MST|CCCD|CMND)?[:\s]*([A-Za-z0-9-]+)/i);
    if (m && m[1]) return m[1].trim();

    // 2. OID.2.5.4.97=...
    m = dn.match(/(?:OID\.)?2\.5\.4\.97\s*=\s*(?:VATVN-|VATMST:|VAT-|MST:)?([A-Za-z0-9-]+)/i);
    if (m && m[1]) return m[1].trim();

    // 3. MST: ...
    m = dn.match(/(?:MST|TIN)\s*[:=]\s*([0-9-]{9,14})/i);
    if (m && m[1]) return m[1].trim();

    // 4. SERIALNUMBER=MST:...
    m = dn.match(/(?:SERIALNUMBER|OID\.2\.5\.4\.5)\s*=\s*(?:MST:)?([A-Za-z0-9-]+)/i);
    if (m && m[1] && m[1].length >= 9) return m[1].trim();

    // 5. In CN: (MST: 038079004321)
    m = dn.match(/(?:MST|CCCD|CMND)[:\s]+([0-9-]{9,14})/i);
    if (m && m[1]) return m[1].trim();

    return '';
  }

  extractAddress(acc) {
    if (!acc) return '';
    if (acc.address && acc.address.trim() !== '') return acc.address.trim();
    return this.extractAddressFromDn(acc.certificateDN) || '';
  }

  extractAddressFromDn(dn) {
    if (!dn) return '';
    const parts = [];

    // STREET=...
    const mStreet = dn.match(/STREET\s*=\s*([^,]+)/i);
    if (mStreet && mStreet[1]) parts.push(mStreet[1].trim());

    // L=...
    const mL = dn.match(/(?:^|[,;])\s*L\s*=\s*([^,]+)/i);
    if (mL && mL[1]) {
      const lVal = mL[1].trim();
      if (!parts.includes(lVal)) parts.push(lVal);
    }

    // ST=... or STATE=...
    const mSt = dn.match(/(?:^|[,;])\s*(?:ST|STATE)\s*=\s*([^,]+)/i);
    if (mSt && mSt[1]) {
      const stVal = mSt[1].trim();
      if (!parts.includes(stVal)) parts.push(stVal);
    }

    return parts.join(', ');
  }

  renderUidsTable() {
    const tbody = document.getElementById('uids-tbody');
    if (!tbody) return;
    const accounts = this.config?.accounts || [];

    // Lấy danh sách các tháng có trong dữ liệu (YYYY-MM)
    const monthSet = new Set();
    accounts.forEach(acc => {
      if (acc.validFrom) {
        const d = new Date(acc.validFrom);
        const m = `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}`;
        monthSet.add(m);
      }
    });
    
    // Cập nhật dropdown nếu chưa có
    const filterSelect = document.getElementById('uids-filter-month');
    if (filterSelect) {
      const currentVal = filterSelect.value;
      const sortedMonths = Array.from(monthSet).sort().reverse(); // Mới nhất lên đầu
      
      let optionsHtml = `<option value="all">Tất cả thời gian</option>`;
      sortedMonths.forEach(m => {
        const parts = m.split('-');
        const label = `Tháng ${parts[1]}/${parts[0]}`;
        optionsHtml += `<option value="${m}">${label}</option>`;
      });
      
      if (filterSelect.getAttribute('data-loaded-months') !== sortedMonths.join(',')) {
        filterSelect.innerHTML = optionsHtml;
        filterSelect.value = currentVal && sortedMonths.includes(currentVal) ? currentVal : 'all';
        filterSelect.setAttribute('data-loaded-months', sortedMonths.join(','));
      }
    }

    // Lọc accounts theo dropdown
    const selectedMonth = filterSelect ? filterSelect.value : 'all';
    const filteredAccounts = selectedMonth === 'all' 
      ? accounts 
      : accounts.filter(acc => {
          if (!acc.validFrom) return false;
          const d = new Date(acc.validFrom);
          const m = `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}`;
          return m === selectedMonth;
        });

    // Cập nhật tổng số lượng
    const countEl = document.getElementById('uids-total-count');
    if (countEl) {
      countEl.textContent = filteredAccounts.length;
    }

    if (filteredAccounts.length === 0) {
      tbody.innerHTML = `<tr><td colspan="11" style="text-align: center; color: var(--text-muted); padding: 24px;">Chưa có tài khoản UID nào phù hợp với bộ lọc.</td></tr>`;
      return;
    }

    const sortedAccounts = [...filteredAccounts].sort((a, b) => {
      const aTime = a.validFrom ? new Date(a.validFrom).getTime() : 0;
      const bTime = b.validFrom ? new Date(b.validFrom).getTime() : 0;
      return bTime - aTime; 
    });

    tbody.innerHTML = sortedAccounts.map((acc, idx) => {
      const taxId = this.extractTaxId(acc);
      const address = this.extractAddress(acc);

      const validFromDate = acc.validFrom ? new Date(acc.validFrom) : null;
      const validToDate = acc.validTo ? new Date(acc.validTo) : null;
      let validToStr = '<span style="color: var(--text-muted); font-size: 0.8rem;">--</span>';
      
      if (validToDate || validFromDate) {
        const isExpired = validToDate && validToDate < new Date();
        const color = isExpired ? '#f87171' : '#38bdf8';
        const iconTo = isExpired ? 'fa-calendar-xmark' : 'fa-calendar-check';
        
        validToStr = `<div style="display: flex; flex-direction: column; gap: 3px; font-size: 0.75rem; text-align: left; justify-content: center; min-width: 80px;">
          ${validFromDate ? `<span style="color: #cbd5e1;" title="Ngày bắt đầu"><i class="fa-regular fa-calendar-plus" style="margin-right: 4px; opacity: 0.7;"></i>${validFromDate.toLocaleDateString('vi-VN')}</span>` : ''}
          ${validToDate ? `<span style="color: ${color};" title="Ngày hết hạn"><i class="fa-regular ${iconTo}" style="margin-right: 4px;"></i>${validToDate.toLocaleDateString('vi-VN')}</span>` : ''}
        </div>`;
      }

      return `
      <tr>
        <td style="font-family: monospace; color: var(--text-muted);">${idx + 1}</td>
        <td>
          <div style="display: flex; align-items: center; gap: 8px;">
            <div>
              <strong>${acc.signerName || 'Chưa đặt tên'}</strong>
              ${acc.department ? `<div style="font-size: 0.75rem; color: var(--text-muted);">${acc.department}</div>` : ''}
            </div>
          </div>
        </td>
        <td><code style="color: var(--accent); font-size: 0.8rem;">${acc.agreementUUID}</code></td>        
        <td>
          ${taxId ? `<span style="color: #f59e0b; font-size: 0.85rem; font-weight: 500;">${taxId}</span>` : '<span style="color: var(--text-muted); font-size: 0.8rem;">--</span>'}
        </td>
        <td>
          <div style="display: flex; align-items: center; gap: 4px;">
            <i class="fa-solid fa-location-dot" 
               style="font-size: 0.85rem; color: #f43f5e; cursor: pointer; opacity: 0.85;" 
               title="Lấy địa chỉ theo MST" 
               onclick="app.fetchSingleAddress('${acc.agreementUUID}')"></i>
            ${address ? `<span style="color: #cbd5e1; font-size: 0.82rem;" title="${address}">${address}</span>` : '<span style="color: var(--text-muted); font-size: 0.8rem;">--</span>'}
          </div>
        </td>
        <td>
          ${acc.phone ? `<span style="color: #38bdf8; font-family: monospace; font-size: 0.82rem; display: inline-flex; align-items: center; gap: 4px;"><i class="fa-solid fa-phone" style="font-size: 0.72rem; opacity: 0.75;"></i>${acc.phone}</span>` : '<span style="color: var(--text-muted); font-size: 0.8rem;">--</span>'}
        </td>
        <td style="text-align: center;">${validToStr}</td>
        <td style="text-align: center;">
          <span class="badge-fmt" style="background: rgba(56, 189, 248, 0.12); color: #38bdf8; border: 1px solid rgba(56, 189, 248, 0.25); padding: 3px 9px; border-radius: 12px; font-weight: 600; font-size: 0.78rem; display: inline-flex; align-items: center; gap: 4px;">
            <i class="fa-solid fa-file-circle-check"></i> ${acc.signedCount || 0}
          </span>
        </td>
        <td><span style="font-family: monospace; color: #cbd5e1;">••••••••</span></td>
        <td><span class="status-badge success"><i class="fa-solid fa-circle-check"></i> ${acc.status || 'Hoạt động'}</span></td>
        <td>
          <div class="actions-cell" style="justify-content: flex-end; gap: 4px;">
            <button type="button" class="btn btn-secondary btn-sm" onclick="app.verifyUidCertificate('${acc.agreementUUID}', '${acc.defaultPasscode}')" title="Kiểm tra chứng thư RSSP">
              <i class="fa-solid fa-certificate"></i>
            </button>
            <button type="button" class="btn btn-outline btn-sm" onclick="app.openEditUidModal('${acc.agreementUUID}')" title="Chỉnh sửa thông tin" style="color: #38bdf8; border-color: rgba(56, 189, 248, 0.3);">
              <i class="fa-solid fa-pen-to-square"></i>
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
      `;
    }).join('');
  }

  async bulkUpdateAddresses() {
    const crmCookieInput = document.getElementById('crm-cookie-input');
    let crmCookie = crmCookieInput ? crmCookieInput.value.trim() : '';
    if (!crmCookie) {
      crmCookie = localStorage.getItem('eSign_crm_cookie') || '';
    }
    if (!crmCookie) {
      this.showToast('Vui lòng nhập Cookie CRM trước khi cập nhật hàng loạt!', 'warning');
      this.openAddUidModal();
      return;
    }

    const accounts = this.config?.accounts || [];
    let updatedCount = 0;

    if (accounts.length === 0) {
      this.showToast('Không có UID nào trong danh sách.', 'info');
      return;
    }

    this.showToast('Đang tiến hành lấy địa chỉ hàng loạt...', 'info');

    for (let i = 0; i < accounts.length; i++) {
      const acc = accounts[i];
      const taxId = this.extractTaxId(acc);
      if (!taxId) continue;

      try {
        const formData = new FormData();
        formData.append('vMST', taxId);
        formData.append('crmCookie', crmCookie);

        const resp = await fetch('/api/proxy/crm-company-info', { method: 'POST', body: formData });
        if (resp.ok) {
          const data = await resp.json();
          if (data && data.length > 0) {
            const info = data[0];
            if (info.Code === "LOGIN") {
              const newCookie = await this.autoRefreshCrmCookie();
              if (newCookie) {
                crmCookie = newCookie;
                if (crmCookieInput) crmCookieInput.value = '';
                i--; // Lùi lại 1 bước để thử lại taxId này
                continue;
              } else {
                this.showToast('Phiên đăng nhập CRM đã hết hạn. Đang dừng tiến trình.', 'error');
                break;
              }
            }
            const name = info.TEN_GOI || info.COMPANY_NAME || info.NAME || '';
            const address = info.DIA_CHI || info.ADDRESS || info.COMPANY_ADDRESS || '';
            const email = info.EMAIL || '';
            const phone = info.DIEN_THOAI || info.PHONE || info.MOBILE || '';

            let changed = false;
            if (name && acc.signerName !== name) { acc.signerName = name; changed = true; }
            if (address && acc.address !== address) { acc.address = address; changed = true; }
            if (email && acc.email !== email) { acc.email = email; changed = true; }
            if (phone && acc.phone !== phone) { acc.phone = phone; changed = true; }

            if (changed) updatedCount++;
          }
        }
      } catch (e) {
        console.error('Lỗi khi cập nhật hàng loạt cho MST ' + taxId, e);
      }
    }

    if (updatedCount > 0) {
      try {
        const resp = await fetch('/api/config', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify(this.config)
        });
        if (resp.ok) {
          this.renderUidsTable();
          this.showToast(`Đã lấy thông tin thành công cho ${updatedCount} UID!`, 'success');
        } else {
          this.showToast('Lỗi lưu cấu hình sau khi cập nhật.', 'error');
        }
      } catch (err) {
        this.showToast('Lỗi lưu cấu hình: ' + err.message, 'error');
      }
    } else {
      this.showToast('Không có thay đổi nào được cập nhật.', 'info');
    }
  }

  async fetchSingleAddress(uuid) {
    const acc = (this.config?.accounts || []).find(a => a.agreementUUID === uuid);
    if (!acc) return;
    const taxId = this.extractTaxId(acc);
    if (!taxId) {
      this.showToast('Tài khoản này không có Mã số thuế (MST)!', 'warning');
      return;
    }

    const crmCookieInput = document.getElementById('crm-cookie-input');
    let crmCookie = crmCookieInput ? crmCookieInput.value.trim() : '';
    if (!crmCookie) {
      crmCookie = localStorage.getItem('eSign_crm_cookie') || '';
    }

    this.showToast(`Đang lấy thông tin cho MST ${taxId}...`, 'info');
    
    const doFetch = async (cookieStr) => {
      const formData = new FormData();
      formData.append('vMST', taxId);
      formData.append('crmCookie', cookieStr);
      const resp = await fetch('/api/proxy/crm-company-info', { method: 'POST', body: formData });
      if (!resp.ok) throw new Error(`HTTP error! status: ${resp.status}`);
      return await resp.json();
    };

    try {
      let data = await doFetch(crmCookie);
      if (data && data.length > 0) {
        let info = data[0];
        if (info.Code === "LOGIN") {
          const newCookie = await this.autoRefreshCrmCookie();
          if (newCookie) {
            crmCookie = newCookie;
            if (crmCookieInput) crmCookieInput.value = '';
            data = await doFetch(crmCookie);
            info = data && data.length > 0 ? data[0] : null;
          } else {
            this.showToast('Phiên đăng nhập CRM đã hết hạn. Vui lòng cập nhật lại chuỗi Cookie!', 'error');
            return;
          }
        }
        
        if (info && info.Code !== "LOGIN") {
          const name = info.TEN_GOI || info.COMPANY_NAME || info.NAME || '';
          const address = info.DIA_CHI || info.ADDRESS || info.COMPANY_ADDRESS || '';
          const email = info.EMAIL || '';
          const phone = info.DIEN_THOAI || info.PHONE || info.MOBILE || '';

          let changed = false;
          if (name && acc.signerName !== name) { acc.signerName = name; changed = true; }
          if (address && acc.address !== address) { acc.address = address; changed = true; }
          if (email && acc.email !== email) { acc.email = email; changed = true; }
          if (phone && acc.phone !== phone) { acc.phone = phone; changed = true; }

          if (changed) {
            const resp = await fetch('/api/config', {
              method: 'POST',
              headers: { 'Content-Type': 'application/json' },
              body: JSON.stringify(this.config)
            });
            if (resp.ok) {
              this.renderUidsTable();
              this.showToast('Đã cập nhật thông tin địa chỉ thành công!', 'success');
            }
          } else {
            this.showToast('Thông tin không có sự thay đổi.', 'info');
          }
        } else {
          this.showToast('Không tìm thấy thông tin cho MST này!', 'warning');
        }
      } else {
        this.showToast('Không tìm thấy thông tin cho MST này!', 'warning');
      }
    } catch (err) {
      this.showToast('Lỗi lấy thông tin: ' + err.message, 'error');
    }
  }

  async autoRefreshCrmCookie() {
    this.showToast('Đang tự động đăng nhập CRM để làm mới phiên...', 'info');
    try {
      const resp = await fetch('/api/proxy/crm-login', { method: 'POST' });
      if (resp.ok) {
        const data = await resp.json();
        if (data.success && data.cookie) {
          localStorage.setItem('eSign_crm_cookie', data.cookie);
          const crmCookieInput = document.getElementById('crm-cookie-input');
          if (crmCookieInput) crmCookieInput.value = data.cookie;
          this.showToast('Tự động đăng nhập & cập nhật Cookie CRM thành công!', 'success');
          return data.cookie;
        }
      }
      this.showToast('Tự động cập nhật Cookie thất bại. Vui lòng kiểm tra lại.', 'error');
      return null;
    } catch (e) {
      this.showToast('Lỗi khi cập nhật Cookie: ' + e.message, 'error');
      return null;
    }
  }

  async fetchCompanyInfo() {
    const taxIdInput = document.getElementById('uid-input-taxid');
    const taxId = taxIdInput.value.trim();
    if (!taxId) {
      this.showToast('Vui lòng nhập Mã số thuế trước!', 'error');
      return;
    }

    const crmCookieInput = document.getElementById('crm-cookie-input');
    const crmCookie = crmCookieInput ? crmCookieInput.value.trim() : '';
    if (crmCookie) {
      localStorage.setItem('eSign_crm_cookie', crmCookie);
    }
    const finalCookie = crmCookie || localStorage.getItem('eSign_crm_cookie') || '';

    this.showToast('Đang lấy thông tin doanh nghiệp từ CRM...', 'info');
    try {
      const formData = new FormData();
      formData.append('vMST', taxId);
      formData.append('crmCookie', finalCookie);

      const resp = await fetch('/api/proxy/crm-company-info', {
        method: 'POST',
        body: formData
      });
      if (!resp.ok) {
        throw new Error(`HTTP error! status: ${resp.status}`);
      }
      const data = await resp.json();
      if (data && data.length > 0) {
        const info = data[0];
        if (info.Code === "LOGIN") {
          const newCookie = await this.autoRefreshCrmCookie();
          if (newCookie) {
            // Xóa input để vòng lặp/fetch dùng cookie trong localStorage
            if (crmCookieInput) crmCookieInput.value = ''; 
            return await this.fetchCompanyInfo();
          }
          this.showToast('Phiên đăng nhập CRM đã hết hạn. Vui lòng cập nhật lại chuỗi Cookie!', 'error');
          return;
        }
        const name = info.TEN_GOI || info.COMPANY_NAME || info.NAME || '';
        const address = info.DIA_CHI || info.ADDRESS || info.COMPANY_ADDRESS || '';
        const email = info.EMAIL || '';
        const phone = info.DIEN_THOAI || info.PHONE || info.MOBILE || '';

        if (name) {
          document.getElementById('uid-input-name').value = name;
        }
        if (address) {
          document.getElementById('uid-input-address').value = address;
        }
        if (email) {
          document.getElementById('uid-input-email').value = email;
        }
        if (phone) {
          document.getElementById('uid-input-phone').value = phone;
        }
        this.showToast('Đã lấy thông tin doanh nghiệp thành công!', 'success');
      } else {
        this.showToast('Không tìm thấy thông tin cho MST này!', 'warning');
      }
    } catch (err) {
      this.showToast('Lỗi lấy thông tin: ' + err.message, 'error');
    }
  }

  openAddUidModal() {
    const titleEl = document.getElementById('modal-uid-title');
    if (titleEl) titleEl.innerHTML = '<i class="fa-solid fa-user-plus"></i> Thêm Tài Khoản UID Mới';
    const uuidInput = document.getElementById('uid-input-uuid');
    uuidInput.value = '';
    uuidInput.removeAttribute('readonly');
    document.getElementById('uid-input-name').value = '';
    document.getElementById('uid-input-dept').value = '';
    document.getElementById('uid-input-taxid').value = '';
    document.getElementById('uid-input-address').value = '';
    document.getElementById('uid-input-email').value = '';
    document.getElementById('uid-input-phone').value = '';
    document.getElementById('uid-input-passcode').value = '12345678';
    document.getElementById('uid-input-status').value = 'Hoạt động';
    const crmCookieInput = document.getElementById('crm-cookie-input');
    if (crmCookieInput) crmCookieInput.value = localStorage.getItem('eSign_crm_cookie') || '';
    this.openModal('modal-add-uid');
  }

  openEditUidModal(uuid) {
    const acc = (this.config?.accounts || []).find(a => a.agreementUUID.toLowerCase() === uuid.toLowerCase());
    if (!acc) return;

    const titleEl = document.getElementById('modal-uid-title');
    if (titleEl) titleEl.innerHTML = '<i class="fa-solid fa-user-pen"></i> Chỉnh Sửa Tài Khoản UID';
    const uuidInput = document.getElementById('uid-input-uuid');
    uuidInput.value = acc.agreementUUID || '';
    document.getElementById('uid-input-name').value = acc.signerName || '';
    document.getElementById('uid-input-dept').value = acc.department || '';
    document.getElementById('uid-input-taxid').value = this.extractTaxId(acc) || '';
    document.getElementById('uid-input-address').value = this.extractAddress(acc) || '';
    document.getElementById('uid-input-email').value = acc.email || '';
    document.getElementById('uid-input-phone').value = acc.phone || '';
    document.getElementById('uid-input-passcode').value = acc.defaultPasscode || '12345678';
    document.getElementById('uid-input-status').value = acc.status || 'Hoạt động';
    const crmCookieInput = document.getElementById('crm-cookie-input');
    if (crmCookieInput) crmCookieInput.value = localStorage.getItem('eSign_crm_cookie') || '';
    this.openModal('modal-add-uid');
  }
  async handleSaveUid(event) {
    event.preventDefault();
    const account = {
      agreementUUID: document.getElementById('uid-input-uuid').value.trim(),
      signerName: document.getElementById('uid-input-name').value.trim(),
      department: document.getElementById('uid-input-dept').value.trim(),
      taxId: document.getElementById('uid-input-taxid').value.trim(),
      address: document.getElementById('uid-input-address').value.trim(),
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
        // Sync verified details back to local accounts
        const existing = (this.config?.accounts || []).find(a => a.agreementUUID.toLowerCase() === uuid.toLowerCase());
        if (existing) {
          if (data.signerName) existing.signerName = data.signerName;
          if (data.taxId) existing.taxId = data.taxId;
          if (data.address) existing.address = data.address;
          if (data.certificateDN) existing.certificateDN = data.certificateDN;
          if (data.serialNumber) existing.certificateSerialNumber = data.serialNumber;
          if (data.issuerDN) existing.issuerDN = data.issuerDN;
          if (data.validFrom) existing.validFrom = data.validFrom;
          if (data.validTo) existing.validTo = data.validTo;
          this.renderUidsTable();
        }

        const body = document.getElementById('modal-cert-body');
        body.innerHTML = `
          <div style="background: rgba(15,23,42,0.6); padding: 16px; border-radius: var(--radius-md); border: 1px solid var(--border-color);">
            <div style="margin-bottom: 10px;"><span style="color: var(--text-muted);">UID:</span> <code>${uuid}</code></div>
            <div style="margin-bottom: 10px;"><span style="color: var(--text-muted);">Tên chủ chứng thư:</span> <strong>${data.signerName || 'N/A'}</strong></div>
            ${data.taxId ? `<div style="margin-bottom: 10px;"><span style="color: var(--text-muted);">Mã số thuế / MST:</span> <code style="color: #f59e0b; font-weight: 600; font-size: 0.9rem;">${data.taxId}</code></div>` : ''}
            ${data.address ? `<div style="margin-bottom: 10px;"><span style="color: var(--text-muted);">Địa chỉ / Tỉnh thành:</span> <span style="color: #e2e8f0; font-weight: 500;">${data.address}</span></div>` : ''}
            <div style="margin-bottom: 10px;"><span style="color: var(--text-muted);">Certificate DN:</span> <div style="font-size: 0.85rem; word-break: break-all; color: #94a3b8; font-family: monospace;">${data.certificateDN || 'N/A'}</div></div>
            <div style="margin-bottom: 10px;"><span style="color: var(--text-muted);">Serial Number:</span> <code style="color: var(--accent);">${data.serialNumber || 'N/A'}</code></div>
            <div style="margin-bottom: 10px;">
              <span style="color: var(--text-muted);">Thời hạn hiệu lực:</span>
              <div style="font-size: 0.85rem; display: flex; flex-direction: column; gap: 3px; margin-top: 4px;">
                <div style="color: #38bdf8;"><i class="fa-regular fa-calendar-check" style="margin-right: 6px;"></i><strong>Hiệu lực từ:</strong> ${data.validFrom ? new Date(data.validFrom).toLocaleString('vi-VN') : 'N/A'}</div>
                <div style="color: #fbbf24;"><i class="fa-regular fa-calendar-xmark" style="margin-right: 6px;"></i><strong>Hiệu lực đến:</strong> ${data.validTo ? new Date(data.validTo).toLocaleString('vi-VN') : 'N/A'}</div>
              </div>
            </div>
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

  // =================== GOOGLE SHEET SYNC ===================

  openGoogleSheetSyncModal() {
    // Reset UI state
    const statusBox = document.getElementById('gsync-status-box');
    const resultsContainer = document.getElementById('gsync-results-container');
    const btnPush = document.getElementById('gsync-btn-push');
    const btnDownload = document.getElementById('gsync-btn-download');
    const btnStart = document.getElementById('gsync-btn-start');

    if (statusBox) statusBox.style.display = 'none';
    if (resultsContainer) resultsContainer.style.display = 'none';
    if (btnPush) btnPush.style.display = 'none';
    if (btnDownload) btnDownload.style.display = 'none';
    if (btnStart) { btnStart.disabled = false; btnStart.innerHTML = '<i class="fa-solid fa-arrows-rotate"></i> 1. Quét & Tra Cứu Tự Động từ RSSP'; }

    this._gSyncLastResult = null;
    this.openModal('modal-googlesheet-sync');
  }

  async handleGoogleSheetSync() {
    const sheetUrl = (document.getElementById('gsync-sheet-url')?.value || '').trim();
    const webhookUrl = (document.getElementById('gsync-webhook-url')?.value || '').trim();

    if (!sheetUrl) {
      this.showToast('Vui lòng nhập đường dẫn Google Sheet!', 'error');
      return;
    }

    const btnStart = document.getElementById('gsync-btn-start');
    const statusBox = document.getElementById('gsync-status-box');
    const resultsContainer = document.getElementById('gsync-results-container');
    const btnPush = document.getElementById('gsync-btn-push');
    const btnDownload = document.getElementById('gsync-btn-download');

    // Show loading state
    if (btnStart) {
      btnStart.disabled = true;
      btnStart.innerHTML = '<i class="fa-solid fa-spinner fa-spin"></i> Đang tra cứu RSSP...';
    }
    if (statusBox) {
      statusBox.style.display = 'block';
      statusBox.innerHTML = `
        <div style="display: flex; align-items: center; gap: 10px; padding: 14px 16px; background: rgba(56,189,248,0.08); border: 1px solid rgba(56,189,248,0.25); border-radius: var(--radius-md); color: #94a3b8;">
          <i class="fa-solid fa-spinner fa-spin" style="color: #38bdf8; font-size: 1.1rem;"></i>
          <span>Đang tải dữ liệu từ Google Sheet và tra cứu RSSP... Vui lòng đợi, quá trình này có thể mất vài giây.</span>
        </div>`;
    }
    if (resultsContainer) resultsContainer.style.display = 'none';
    if (btnPush) btnPush.style.display = 'none';
    if (btnDownload) btnDownload.style.display = 'none';

    try {
      const resp = await fetch('/api/admin/googlesheet/sync', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ sheetUrl, webhookUrl, autoSaveToAccounts: true })
      });

      const data = await resp.json();
      this._gSyncLastResult = data;

      if (!data.success) {
        if (statusBox) {
          statusBox.innerHTML = `
            <div style="padding: 14px 16px; background: rgba(239,68,68,0.1); border: 1px solid rgba(239,68,68,0.3); border-radius: var(--radius-md);">
              <div style="display: flex; align-items: center; gap: 8px; margin-bottom: 6px; color: #f87171; font-weight: 600;">
                <i class="fa-solid fa-circle-xmark"></i> Lỗi đồng bộ
              </div>
              <p style="color: #cbd5e1; font-size: 0.875rem; margin: 0;">${data.message || 'Không thể kết nối hoặc đọc dữ liệu từ Google Sheet.'}</p>
            </div>`;
        }
        if (btnStart) { btnStart.disabled = false; btnStart.innerHTML = '<i class="fa-solid fa-arrows-rotate"></i> 1. Quét & Tra Cứu Tự Động từ RSSP'; }
        return;
      }

      // Success summary
      const updCount = data.updatedCount || 0;
      const errCount = data.errorCount || 0;
      const totalCount = data.processedCount || 0;
      const hasWebhook = !!webhookUrl;

      if (statusBox) {
        statusBox.innerHTML = `
          <div style="padding: 14px 16px; background: rgba(34,197,94,0.08); border: 1px solid rgba(34,197,94,0.3); border-radius: var(--radius-md);">
            <div style="display: flex; align-items: center; gap: 8px; margin-bottom: 10px; color: #4ade80; font-weight: 600;">
              <i class="fa-solid fa-circle-check"></i> Đồng bộ thành công – ${data.message || ''}
            </div>
            <div style="display: flex; gap: 16px; flex-wrap: wrap;">
              <div style="background: rgba(15,23,42,0.5); border: 1px solid var(--border-color); border-radius: var(--radius-sm); padding: 8px 14px; text-align: center; min-width: 80px;">
                <div style="font-size: 1.4rem; font-weight: 700; color: #f8fafc;">${totalCount}</div>
                <div style="font-size: 0.72rem; color: var(--text-muted);">Đã xử lý</div>
              </div>
              <div style="background: rgba(15,23,42,0.5); border: 1px solid rgba(34,197,94,0.35); border-radius: var(--radius-sm); padding: 8px 14px; text-align: center; min-width: 80px;">
                <div style="font-size: 1.4rem; font-weight: 700; color: #4ade80;">${updCount}</div>
                <div style="font-size: 0.72rem; color: var(--text-muted);">Thành công</div>
              </div>
              <div style="background: rgba(15,23,42,0.5); border: 1px solid rgba(239,68,68,0.3); border-radius: var(--radius-sm); padding: 8px 14px; text-align: center; min-width: 80px;">
                <div style="font-size: 1.4rem; font-weight: 700; color: #f87171;">${errCount}</div>
                <div style="font-size: 0.72rem; color: var(--text-muted);">Lỗi</div>
              </div>
            </div>
            ${data.webhookSent !== undefined ? `<div style="margin-top: 10px; font-size: 0.8rem; color: ${data.webhookSent ? '#4ade80' : '#f87171'};">
              <i class="fa-solid fa-${data.webhookSent ? 'cloud-arrow-up' : 'triangle-exclamation'}"></i>
              Webhook: ${data.webhookMessage || (data.webhookSent ? 'Đã gửi' : 'Không gửi')}
            </div>` : ''}
          </div>`;
      }

      // Render rows table
      if (resultsContainer && data.rows && data.rows.length > 0) {
        resultsContainer.style.display = 'block';
        document.getElementById('gsync-count-total').textContent = data.rows.length;
        const tbody = document.getElementById('gsync-results-tbody');
        if (tbody) {
          tbody.innerHTML = data.rows.map(row => {
            const isOk = row.isSuccess;
            const nameChanged = row.newName && row.newName !== row.oldName;
            const taxChanged = row.newTaxId && row.newTaxId !== row.oldTaxId;
            const hasNewDate = !!row.newDate;

            const dateHtml = hasNewDate
              ? `<div style="display: flex; flex-direction: column; gap: 3px; font-size: 0.75rem; text-align: left; justify-content: center; min-width: 80px;">
                  <span style="color: #cbd5e1;" title="Ngày bắt đầu"><i class="fa-regular fa-calendar-plus" style="margin-right: 4px; opacity: 0.7;"></i>${row.newDate}</span>
                  ${row.newEndDate ? `<span style="color: #38bdf8;" title="Ngày hết hạn"><i class="fa-regular fa-calendar-check" style="margin-right: 4px;"></i>${row.newEndDate}</span>` : ''}
                 </div>`
              : `<span style="color: var(--text-muted); font-size: 0.8rem;">--</span>`;

            const nameHtml = nameChanged
              ? `<span style="color: #4ade80; font-weight: 600;">${row.newName}</span> <span style="color: var(--text-muted); font-size: 0.75rem; text-decoration: line-through;">${row.oldName || ''}</span>`
              : `<span style="color: #e2e8f0;">${row.newName || row.oldName || '--'}</span>`;

            const taxHtml = taxChanged
              ? `<span style="color: #fbbf24; font-weight: 600; font-family: monospace;">${row.newTaxId}</span>`
              : `<span style="color: ${row.newTaxId || row.oldTaxId ? '#f59e0b' : 'var(--text-muted)'}; font-family: monospace;">${row.newTaxId || row.oldTaxId || '--'}</span>`;

            const statusBadge = isOk
              ? `<span style="background: rgba(34,197,94,0.15); color: #4ade80; border: 1px solid rgba(34,197,94,0.3); border-radius: 4px; padding: 2px 8px; font-size: 0.75rem; white-space: nowrap;">✓ OK</span>`
              : `<span style="background: rgba(239,68,68,0.12); color: #f87171; border: 1px solid rgba(239,68,68,0.25); border-radius: 4px; padding: 2px 8px; font-size: 0.75rem; white-space: nowrap;" title="${row.message || ''}">✗ Lỗi</span>`;

            return `<tr style="opacity: ${isOk ? '1' : '0.65'};">
              <td style="text-align: center; color: var(--text-muted);">${row.rowIndex}</td>
              <td style="font-family: monospace; font-size: 0.78rem; color: #38bdf8;" title="${row.agreementUUID}">${(row.agreementUUID || '').substring(0, 18)}…</td>
              <td style="font-family: monospace; font-size: 0.78rem; color: var(--text-muted);">${row.passcode || '--'}</td>
              <td>${dateHtml}</td>
              <td>${nameHtml}</td>
              <td>'${taxHtml}</td>
              <td>${statusBadge}</td>
            </tr>`;
          }).join('');
        }
      }

      // Show push/download buttons
      if (btnPush && hasWebhook) btnPush.style.display = '';
      if (btnDownload && data.rows && data.rows.length > 0) btnDownload.style.display = '';

      // Refresh the UID table in background
      await this.loadConfig();

      this.showToast(`Đồng bộ xong: ${updCount} thành công, ${errCount} lỗi.`, updCount > 0 ? 'success' : 'info', 5000);
    } catch (err) {
      if (statusBox) {
        statusBox.innerHTML = `
          <div style="padding: 14px 16px; background: rgba(239,68,68,0.1); border: 1px solid rgba(239,68,68,0.3); border-radius: var(--radius-md); color: #f87171;">
            <i class="fa-solid fa-triangle-exclamation"></i> Lỗi kết nối máy chủ: ${err.message}
          </div>`;
      }
      this.showToast('Lỗi: ' + err.message, 'error');
    } finally {
      if (btnStart) {
        btnStart.disabled = false;
        btnStart.innerHTML = '<i class="fa-solid fa-arrows-rotate"></i> 1. Quét & Tra Cứu Tự Động từ RSSP';
      }
    }
  }

  async handlePushToGoogleSheet() {
    const webhookUrl = (document.getElementById('gsync-webhook-url')?.value || '').trim();
    if (!webhookUrl) {
      this.showToast('Vui lòng nhập Webhook URL trước!', 'error');
      return;
    }
    if (!this._gSyncLastResult?.rows?.length) {
      this.showToast('Chưa có dữ liệu để đẩy. Hãy chạy Quét & Tra Cứu trước.', 'warning');
      return;
    }

    const btnPush = document.getElementById('gsync-btn-push');
    if (btnPush) { btnPush.disabled = true; btnPush.innerHTML = '<i class="fa-solid fa-spinner fa-spin"></i> Đang đẩy lên Sheet...'; }

    try {
      const resp = await fetch('/api/admin/googlesheet/push', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          webhookUrl,
          gid: this._gSyncLastResult.gid || '0',
          updates: this._gSyncLastResult.rows
            .filter(r => r.isSuccess)
            .map(r => ({
              row: r.rowIndex,
              date: r.newDate || '',             // Cột B – Ngày bắt đầu sử dụng
              name: r.newName || r.oldName,      // Cột C – HKD
              taxId: r.newTaxId || r.oldTaxId,  // Cột D – MST
              address: r.newAddress || r.oldAddress, // Cột E – Địa chỉ
              uuid: r.agreementUUID,
              passcode: r.passcode
            }))
        })
      });
      const data = await resp.json();
      if (data.success) {
        this.showToast('✓ ' + (data.message || 'Đã đẩy dữ liệu lên Google Sheet thành công!'), 'success', 6000);
      } else {
        this.showToast('✗ ' + (data.message || 'Webhook phản hồi lỗi.'), 'error', 6000);
      }
    } catch (err) {
      this.showToast('Lỗi đẩy webhook: ' + err.message, 'error');
    } finally {
      if (btnPush) { btnPush.disabled = false; btnPush.innerHTML = '<i class="fa-solid fa-cloud-arrow-up"></i> 2. Đẩy Dữ Liệu Lên Google Sheet (Webhook)'; }
    }
  }

  async handleDownloadUpdatedCsv() {
    const btnDownload = document.getElementById('gsync-btn-download');
    if (btnDownload) { btnDownload.disabled = true; btnDownload.innerHTML = '<i class="fa-solid fa-spinner fa-spin"></i> Đang tải...'; }

    try {
      const resp = await fetch('/api/admin/googlesheet/export-csv', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ rows: this._gSyncLastResult?.rows || [] })
      });

      if (!resp.ok) throw new Error('Server phản hồi lỗi: ' + resp.status);

      const blob = await resp.blob();
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `google-sheet-updated-${new Date().toISOString().slice(0, 10)}.csv`;
      document.body.appendChild(a);
      a.click();
      document.body.removeChild(a);
      URL.revokeObjectURL(url);
      this.showToast('Đã tải file CSV thành công!', 'success');
    } catch (err) {
      this.showToast('Lỗi tải CSV: ' + err.message, 'error');
    } finally {
      if (btnDownload) { btnDownload.disabled = false; btnDownload.innerHTML = '<i class="fa-solid fa-file-csv"></i> 3. Tải File CSV Đã Điền Đầy Đủ'; }
    }
  }

  toggleAppsScriptGuide() {
    const guideBox = document.getElementById('gsync-guide-box');
    if (!guideBox) return;
    const isHidden = guideBox.style.display === 'none';
    guideBox.style.display = isHidden ? 'block' : 'none';
    if (isHidden) {
      const scriptArea = document.getElementById('gsync-script-code');
      if (scriptArea && !scriptArea.value) {
        scriptArea.value = `// Google Apps Script - Dán vào Extensions > Apps Script > Deploy as Web App
function doPost(e) {
  try {
    var data = JSON.parse(e.postData.contents);
    var ss = SpreadsheetApp.getActiveSpreadsheet();
    var sheet = (data.gid
      ? getSheetByGid_(ss, data.gid)
      : ss.getSheetByName(data.sheetName || "Sheet1")) || ss.getActiveSheet();
    if (data.action === "updateSheet" && data.updates) {
      data.updates.forEach(function(upd) {
        if (!upd.row) return;
        // Cột B (2): Ngày bắt đầu sử dụng
        if (upd.date)    sheet.getRange(upd.row, 2).setValue(upd.date);
        // Cột C (3): HKD – Tên hộ kinh doanh
        if (upd.name)    sheet.getRange(upd.row, 3).setValue(upd.name);
        // Cột D (4): MST – Mã số thuế
        if (upd.taxId)   sheet.getRange(upd.row, 4).setValue(upd.taxId);
      });
    }
    return ContentService.createTextOutput(JSON.stringify({ok: true, updated: (data.updates||[]).length}))
      .setMimeType(ContentService.MimeType.JSON);
  } catch(err) {
    return ContentService.createTextOutput(JSON.stringify({ok: false, error: err.toString()}))
      .setMimeType(ContentService.MimeType.JSON);
  }
}

function getSheetByGid_(ss, gid) {
  var sheets = ss.getSheets();
  for (var i = 0; i < sheets.length; i++) {
    if (String(sheets[i].getSheetId()) === String(gid)) return sheets[i];
  }
  return null;
}`;
      }
    }
  }

  copyAppsScriptCode() {
    const scriptArea = document.getElementById('gsync-script-code');
    if (!scriptArea || !scriptArea.value) {
      this.showToast('Hãy mở hướng dẫn trước để tải mã script!', 'warning');
      return;
    }
    navigator.clipboard.writeText(scriptArea.value).then(() => {
      this.showToast('Đã sao chép mã Apps Script vào clipboard!', 'success');
    }).catch(() => {
      scriptArea.select();
      document.execCommand('copy');
      this.showToast('Đã sao chép!', 'success');
    });
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
