window.stockSanti = window.stockSanti || {};

window.stockSanti.downloadFileFromStream = async (fileName, contentStreamReference) => {
  const arrayBuffer = await contentStreamReference.arrayBuffer();
  window.stockSanti.downloadBlob(fileName, arrayBuffer);
};

window.stockSanti.downloadBlob = (fileName, data, mimeType) => {
  const blob = new Blob([data], {
    type: mimeType || 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet'
  });
  const url = URL.createObjectURL(blob);
  const anchor = document.createElement('a');
  anchor.href = url;
  anchor.download = fileName || 'archivo.xlsx';
  anchor.click();
  anchor.remove();
  URL.revokeObjectURL(url);
};

window.stockSanti.printPage = () => window.print();

window.stockSanti.scrollToElement = (elementId) => {
  const element = document.getElementById(elementId);
  if (element) {
    element.scrollIntoView({ behavior: 'smooth', block: 'start' });
  }
};

window.stockSanti.openInNewTab = (url) => {
  window.open(url, '_blank');
};

window.stockSanti.openDialog = (dialog) => {
  if (!dialog || dialog.open) {
    return;
  }
  dialog.showModal();
};

window.stockSanti.closeDialog = (dialog) => {
  if (!dialog || !dialog.open) {
    return;
  }
  dialog.close();
};
