window.stockSanti = {
  downloadFileFromStream: async (fileName, contentStreamReference) => {
    const arrayBuffer = await contentStreamReference.arrayBuffer();
    const blob = new Blob([arrayBuffer], {
      type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet'
    });
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = fileName || 'reporte.xlsx';
    anchor.click();
    anchor.remove();
    URL.revokeObjectURL(url);
  },
  printPage: () => window.print()
};
