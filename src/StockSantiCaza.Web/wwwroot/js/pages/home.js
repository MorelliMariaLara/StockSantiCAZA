document.addEventListener('DOMContentLoaded', async () => {
  const user = await app.initShell({ activePath: '/', title: 'Dashboard', requireAdmin: true });
  if (!user) return;

  const container = document.getElementById('dashboard-content');
  try {
    const resumen = await api.get('/api/reportes/dashboard');
    let html = `<section class="grid cards">
      <div class="card"><span class="metric-label">Ventas del día</span><strong>${resumen.cantidadVentas}</strong></div>
      <div class="card"><span class="metric-label">Total vendido (USD)</span><strong>${app.formatUsd(resumen.totalVentas)}</strong></div>
      <div class="card"><span class="metric-label">Ganancia del día (USD)</span><strong>${app.formatUsd(resumen.gananciaTotal)}</strong></div>
      <div class="card warning-card"><span class="metric-label">Productos con stock mínimo</span><strong>${resumen.productosConStockMinimo}</strong></div>
      <div class="card"><span class="metric-label">Movimientos de stock</span><strong>${resumen.movimientosStock}</strong></div>
    </section>`;

    if (resumen.alertasStock?.length) {
      html += `<section class="panel"><h2>Alertas de stock mínimo</h2><div class="table-wrap"><table>
        <thead><tr><th>SKU</th><th>Producto</th><th>Stock</th><th>Mínimo</th></tr></thead>
        <tbody>${resumen.alertasStock.map(a => `<tr class="danger-row">
          <td>${app.display(a.sku)}</td><td>${app.display(a.nombre)}</td>
          <td>${a.stockActual}</td><td>${a.stockMinimo}</td></tr>`).join('')}
        </tbody></table></div></section>`;
    }

    container.innerHTML = html;
  } catch (err) {
    container.innerHTML = '';
    app.renderAlerts(document.getElementById('page-alerts'), { error: err.message });
  }
});
