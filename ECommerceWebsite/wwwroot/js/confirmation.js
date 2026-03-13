// Confirmation page scripts isolated to avoid Razor inline issues
(function(){
  'use strict';

  function resendConfirmation(){
    try { toastr.info('Confirmation email sent!'); } catch(_) { alert('Confirmation email sent!'); }
  }

  // Print styles
  window.addEventListener('beforeprint', function(){ document.body.classList.add('printing'); });
  window.addEventListener('afterprint', function(){ document.body.classList.remove('printing'); });

  function downloadOrderDetails(){
    try {
      var el = document.getElementById('order-details');
      if (!el) return;
      var orderNo = (document.querySelector('#order-details .card-body')?.getAttribute('data-order-number')) || 'order';
      var html = "<!doctype html><html><head><meta charset='utf-8'><title>Order " + orderNo + "</title>"+
        "<meta name='viewport' content='width=device-width, initial-scale=1'>"+
        "<link rel='stylesheet' href='https://cdn.jsdelivr.net/npm/bootstrap@4.6.2/dist/css/bootstrap.min.css'>"+
        "<style>body{padding:16px} .card{box-shadow:none!important;border:1px solid #eee}</style>"+
        "</head><body>"+ el.outerHTML +"</body></html>";
      var blob = new Blob([html], {type:'text/html;charset=utf-8'});
      var url = URL.createObjectURL(blob);
      var a = document.createElement('a');
      a.href = url; a.download = 'order-' + orderNo + '.html';
      document.body.appendChild(a); a.click();
      setTimeout(function(){ URL.revokeObjectURL(url); a.remove(); }, 0);
    } catch(e){ console.error('Download failed', e); }
  }

  async function downloadOrderPdf(){
    try {
      var el = document.getElementById('order-details');
      if (!el) return;
      var orderNo = (document.querySelector('#order-details .card-body')?.getAttribute('data-order-number')) || 'order';
      var canvas = await html2canvas(el, { scale: 2, useCORS: true, backgroundColor: '#ffffff', windowWidth: document.documentElement.clientWidth });
      var imgData = canvas.toDataURL('image/png');
      var jspdf = window.jspdf; // UMD
      var pdf = new jspdf.jsPDF('p','pt','a4');
      var pageWidth = pdf.internal.pageSize.getWidth();
      var pageHeight = pdf.internal.pageSize.getHeight();
      var imgWidth = pageWidth;
      var imgHeight = canvas.height * imgWidth / canvas.width;

      var heightLeft = imgHeight;
      var position = 0;

      pdf.addImage(imgData, 'PNG', 0, position, imgWidth, imgHeight);
      heightLeft -= pageHeight;

      while (heightLeft > 0) {
        position = -(imgHeight - heightLeft);
        pdf.addPage();
        pdf.addImage(imgData, 'PNG', 0, position, imgWidth, imgHeight);
        heightLeft -= pageHeight;
      }

      pdf.save('order-' + orderNo + '.pdf');
    } catch(e){
      console.error('PDF generation failed', e);
      try { toastr.error('Failed to generate PDF'); } catch(_) { alert('Failed to generate PDF'); }
    }
  }

  function bindOrderButtons(){
    try {
      var htmlBtn = document.getElementById('btn-download-order');
      var pdfBtn = document.getElementById('btn-download-pdf');
      if (htmlBtn && !htmlBtn._bound) { htmlBtn.addEventListener('click', downloadOrderDetails); htmlBtn._bound = true; }
      if (pdfBtn && !pdfBtn._bound) { pdfBtn.addEventListener('click', downloadOrderPdf); pdfBtn._bound = true; }
    } catch(e){ console.warn('Binding order buttons failed', e); }
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', bindOrderButtons);
  } else {
    bindOrderButtons();
  }

  // expose for debugging if needed
  window.downloadOrderDetails = downloadOrderDetails;
  window.downloadOrderPdf = downloadOrderPdf;
})();