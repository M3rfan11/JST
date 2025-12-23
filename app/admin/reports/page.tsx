"use client"

import { useState, useEffect } from "react"
import { 
  BarChart3, TrendingUp, Users, Package, DollarSign, Tag, 
  ShoppingCart, Calendar, RefreshCw, ArrowUpRight, ArrowDownRight,
  AlertTriangle, Clock
} from "lucide-react"
import { api } from "@/lib/api-client"
import { Button } from "@/components/ui/button"
import {
  BarChart, Bar, LineChart, Line, PieChart, Pie, Cell,
  XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer,
  AreaChart, Area
} from "recharts"

// Types
interface RevenueDataPoint {
  date: string
  label: string
  revenue: number
  orderCount: number
  averageOrderValue: number
}

interface CategorySalesData {
  categoryId: number
  categoryName: string
  totalRevenue: number
  totalQuantity: number
  orderCount: number
  percentageOfTotal: number
}

interface TopCustomerData {
  customerId: number
  customerName: string
  email: string
  totalSpent: number
  orderCount: number
  lastOrderDate: string
  averageOrderValue: number
}

interface CustomerAcquisitionData {
  date: string
  label: string
  newCustomers: number
}

interface ProductMarginData {
  productId: number
  productName: string
  price: number
  cost: number
  grossMargin: number
  grossMarginPercentage: number
  totalRevenue: number
  totalProfit: number
  quantitySold: number
}

interface PromoCodePerformanceData {
  promoCodeId: number
  code: string
  discountType: string
  discountValue: number
  usageCount: number
  usageLimit: number
  totalDiscountGiven: number
  revenueGenerated: number
  roi: number
  isActive: boolean
  expiryDate: string | null
}

interface OrderStatusData {
  status: string
  count: number
  percentage: number
  totalValue: number
}

interface DeadStockData {
  productId: number
  productName: string
  sku: string
  currentStock: number
  stockValue: number
  daysWithoutSale: number
  lastSaleDate: string | null
}

interface StockForecastData {
  productId: number
  productName: string
  currentStock: number
  averageDailySales: number
  daysUntilStockout: number
  predictedStockoutDate: string | null
  riskLevel: string
}

const COLORS = ['#3D0811', '#7C3238', '#B4565E', '#D98E94', '#F5C4C9', '#2E7D32', '#1976D2', '#7B1FA2']

export default function ReportsPage() {
  const [activeTab, setActiveTab] = useState<'sales' | 'customers' | 'inventory' | 'financial' | 'promo' | 'orders'>('sales')
  const [loading, setLoading] = useState(true)
  const [period, setPeriod] = useState<'daily' | 'weekly' | 'monthly'>('daily')
  const [year, setYear] = useState(new Date().getFullYear())
  
  // Data states
  const [revenueData, setRevenueData] = useState<RevenueDataPoint[]>([])
  const [categoryData, setCategoryData] = useState<CategorySalesData[]>([])
  const [customerData, setCustomerData] = useState<any>(null)
  const [inventoryData, setInventoryData] = useState<any>(null)
  const [financialData, setFinancialData] = useState<any>(null)
  const [promoData, setPromoData] = useState<any>(null)
  const [orderStatusData, setOrderStatusData] = useState<any>(null)
  const [salesReport, setSalesReport] = useState<any>(null)

  useEffect(() => {
    loadReportData()
  }, [activeTab, period, year])

  const loadReportData = async () => {
    setLoading(true)
    try {
      switch (activeTab) {
        case 'sales':
          const [revenue, categories, sales] = await Promise.all([
            api.reports.getRevenueOverTime({ period }) as Promise<any>,
            api.reports.getCategorySales({ year }) as Promise<any>,
            api.reports.getSalesReport({ year }) as Promise<any>
          ])
          setRevenueData(revenue.data || [])
          setCategoryData(categories.categories || [])
          setSalesReport(sales)
          break
        case 'customers':
          const customers = await api.reports.getCustomerAnalytics() as any
          setCustomerData(customers)
          break
        case 'inventory':
          const inventory = await api.reports.getInventoryReport({ deadStockDays: 30 }) as any
          setInventoryData(inventory)
          break
        case 'financial':
          const financial = await api.reports.getFinancialReport({ year }) as any
          setFinancialData(financial)
          break
        case 'promo':
          const promo = await api.reports.getPromoAnalytics() as any
          setPromoData(promo)
          break
        case 'orders':
          const orders = await api.reports.getOrderStatusDistribution({ year }) as any
          setOrderStatusData(orders)
          break
      }
    } catch (error) {
      console.error("Error loading report data:", error)
    } finally {
      setLoading(false)
    }
  }

  const formatCurrency = (value: number) => `${value.toLocaleString('en-EG', { minimumFractionDigits: 2 })} EGP`

  const tabs = [
    { id: 'sales', label: 'Sales', icon: TrendingUp },
    { id: 'customers', label: 'Customers', icon: Users },
    { id: 'inventory', label: 'Inventory', icon: Package },
    { id: 'financial', label: 'Financial', icon: DollarSign },
    { id: 'promo', label: 'Promo Codes', icon: Tag },
    { id: 'orders', label: 'Orders', icon: ShoppingCart },
  ]

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-semibold mb-2" style={{ fontFamily: '"Dream Avenue"', color: '#3D0811' }}>
            Reports & Analytics
          </h1>
          <p className="text-muted-foreground" style={{ fontFamily: '"Dream Avenue"' }}>
            Comprehensive insights into your business performance
          </p>
        </div>
        <Button onClick={loadReportData} variant="outline" className="gap-2">
          <RefreshCw className="h-4 w-4" />
          Refresh
        </Button>
      </div>

      {/* Tabs */}
      <div className="flex gap-2 overflow-x-auto pb-2">
        {tabs.map((tab) => {
          const Icon = tab.icon
          return (
            <Button
              key={tab.id}
              variant={activeTab === tab.id ? "default" : "outline"}
              onClick={() => setActiveTab(tab.id as any)}
              className="gap-2 whitespace-nowrap"
              style={activeTab === tab.id ? { backgroundColor: '#3D0811' } : {}}
            >
              <Icon className="h-4 w-4" />
              {tab.label}
            </Button>
          )
        })}
      </div>

      {/* Loading State */}
      {loading && (
        <div className="flex items-center justify-center h-64">
          <div className="text-lg" style={{ fontFamily: '"Dream Avenue"' }}>Loading reports...</div>
        </div>
      )}

      {/* Sales Tab */}
      {!loading && activeTab === 'sales' && (
        <div className="space-y-6">
          {/* Period Selector */}
          <div className="flex gap-2">
            {(['daily', 'weekly', 'monthly'] as const).map((p) => (
              <Button
                key={p}
                variant={period === p ? "default" : "outline"}
                size="sm"
                onClick={() => setPeriod(p)}
                style={period === p ? { backgroundColor: '#3D0811' } : {}}
              >
                {p.charAt(0).toUpperCase() + p.slice(1)}
              </Button>
            ))}
          </div>

          {/* Summary Cards */}
          {salesReport && (
            <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
              <div className="bg-white rounded-lg p-6 border">
                <div className="flex items-center justify-between">
                  <div>
                    <p className="text-sm text-muted-foreground" style={{ fontFamily: '"Dream Avenue"' }}>Total Sales</p>
                    <p className="text-2xl font-semibold" style={{ color: '#3D0811' }}>{formatCurrency(salesReport.totalSales)}</p>
                  </div>
                  <div className="p-3 rounded-lg" style={{ backgroundColor: '#3D0811' }}>
                    <DollarSign className="h-6 w-6 text-white" />
                  </div>
                </div>
              </div>
              <div className="bg-white rounded-lg p-6 border">
                <div className="flex items-center justify-between">
                  <div>
                    <p className="text-sm text-muted-foreground" style={{ fontFamily: '"Dream Avenue"' }}>Total Orders</p>
                    <p className="text-2xl font-semibold" style={{ color: '#3D0811' }}>{salesReport.totalOrders}</p>
                  </div>
                  <div className="p-3 rounded-lg bg-blue-500">
                    <ShoppingCart className="h-6 w-6 text-white" />
                  </div>
                </div>
              </div>
              <div className="bg-white rounded-lg p-6 border">
                <div className="flex items-center justify-between">
                  <div>
                    <p className="text-sm text-muted-foreground" style={{ fontFamily: '"Dream Avenue"' }}>Avg Order Value</p>
                    <p className="text-2xl font-semibold" style={{ color: '#3D0811' }}>{formatCurrency(salesReport.averageOrderValue)}</p>
                  </div>
                  <div className="p-3 rounded-lg bg-green-500">
                    <TrendingUp className="h-6 w-6 text-white" />
                  </div>
                </div>
              </div>
              <div className="bg-white rounded-lg p-6 border">
                <div className="flex items-center justify-between">
                  <div>
                    <p className="text-sm text-muted-foreground" style={{ fontFamily: '"Dream Avenue"' }}>Top Products</p>
                    <p className="text-2xl font-semibold" style={{ color: '#3D0811' }}>{salesReport.topProducts?.length || 0}</p>
                  </div>
                  <div className="p-3 rounded-lg bg-purple-500">
                    <Package className="h-6 w-6 text-white" />
                  </div>
                </div>
              </div>
            </div>
          )}

          {/* Revenue Chart */}
          <div className="bg-white rounded-lg p-6 border">
            <h3 className="text-lg font-semibold mb-4" style={{ fontFamily: '"Dream Avenue"', color: '#3D0811' }}>
              Revenue Over Time
            </h3>
            <ResponsiveContainer width="100%" height={300}>
              <AreaChart data={revenueData}>
                <defs>
                  <linearGradient id="colorRevenue" x1="0" y1="0" x2="0" y2="1">
                    <stop offset="5%" stopColor="#3D0811" stopOpacity={0.8}/>
                    <stop offset="95%" stopColor="#3D0811" stopOpacity={0}/>
                  </linearGradient>
                </defs>
                <CartesianGrid strokeDasharray="3 3" />
                <XAxis dataKey="label" tick={{ fontSize: 12 }} />
                <YAxis tick={{ fontSize: 12 }} />
                <Tooltip formatter={(value: number) => formatCurrency(value)} />
                <Area type="monotone" dataKey="revenue" stroke="#3D0811" fillOpacity={1} fill="url(#colorRevenue)" />
              </AreaChart>
            </ResponsiveContainer>
          </div>

          {/* Category Sales & Top Products */}
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
            {/* Category Sales Pie */}
            <div className="bg-white rounded-lg p-6 border">
              <h3 className="text-lg font-semibold mb-4" style={{ fontFamily: '"Dream Avenue"', color: '#3D0811' }}>
                Sales by Category
              </h3>
              <ResponsiveContainer width="100%" height={300}>
                <PieChart>
                  <Pie
                    data={categoryData}
                    cx="50%"
                    cy="50%"
                    labelLine={false}
                    label={({ categoryName, percentageOfTotal }) => `${categoryName}: ${percentageOfTotal.toFixed(1)}%`}
                    outerRadius={100}
                    fill="#8884d8"
                    dataKey="totalRevenue"
                    nameKey="categoryName"
                  >
                    {categoryData.map((entry, index) => (
                      <Cell key={`cell-${index}`} fill={COLORS[index % COLORS.length]} />
                    ))}
                  </Pie>
                  <Tooltip formatter={(value: number) => formatCurrency(value)} />
                </PieChart>
              </ResponsiveContainer>
            </div>

            {/* Top Products */}
            <div className="bg-white rounded-lg p-6 border">
              <h3 className="text-lg font-semibold mb-4" style={{ fontFamily: '"Dream Avenue"', color: '#3D0811' }}>
                Top Selling Products
              </h3>
              <div className="space-y-3">
                {salesReport?.topProducts?.slice(0, 5).map((product: any, index: number) => (
                  <div key={product.productId} className="flex items-center justify-between p-3 bg-gray-50 rounded-lg">
                    <div className="flex items-center gap-3">
                      <span className="w-6 h-6 rounded-full flex items-center justify-center text-white text-sm" style={{ backgroundColor: COLORS[index] }}>
                        {index + 1}
                      </span>
                      <div>
                        <p className="font-medium text-sm">{product.productName}</p>
                        <p className="text-xs text-muted-foreground">{product.totalQuantity} sold</p>
                      </div>
                    </div>
                    <p className="font-semibold text-sm" style={{ color: '#3D0811' }}>{formatCurrency(product.totalRevenue)}</p>
                  </div>
                ))}
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Customers Tab */}
      {!loading && activeTab === 'customers' && customerData && (
        <div className="space-y-6">
          {/* Summary Cards */}
          <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
            <div className="bg-white rounded-lg p-6 border">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-sm text-muted-foreground" style={{ fontFamily: '"Dream Avenue"' }}>Total Customers</p>
                  <p className="text-2xl font-semibold" style={{ color: '#3D0811' }}>{customerData.totalCustomers}</p>
                </div>
                <Users className="h-8 w-8 text-blue-500" />
              </div>
            </div>
            <div className="bg-white rounded-lg p-6 border">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-sm text-muted-foreground" style={{ fontFamily: '"Dream Avenue"' }}>New This Month</p>
                  <p className="text-2xl font-semibold" style={{ color: '#3D0811' }}>{customerData.newCustomersThisMonth}</p>
                </div>
                <div className="flex items-center gap-1 text-green-500">
                  {customerData.customerGrowthPercentage >= 0 ? <ArrowUpRight className="h-5 w-5" /> : <ArrowDownRight className="h-5 w-5" />}
                  <span className="text-sm">{customerData.customerGrowthPercentage.toFixed(1)}%</span>
                </div>
              </div>
            </div>
            <div className="bg-white rounded-lg p-6 border">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-sm text-muted-foreground" style={{ fontFamily: '"Dream Avenue"' }}>Avg Lifetime Value</p>
                  <p className="text-2xl font-semibold" style={{ color: '#3D0811' }}>{formatCurrency(customerData.averageCustomerLifetimeValue)}</p>
                </div>
                <DollarSign className="h-8 w-8 text-green-500" />
              </div>
            </div>
            <div className="bg-white rounded-lg p-6 border">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-sm text-muted-foreground" style={{ fontFamily: '"Dream Avenue"' }}>Repeat Rate</p>
                  <p className="text-2xl font-semibold" style={{ color: '#3D0811' }}>{customerData.repeatPurchaseRate.toFixed(1)}%</p>
                </div>
                <RefreshCw className="h-8 w-8 text-purple-500" />
              </div>
            </div>
          </div>

          {/* Customer Acquisition Chart */}
          <div className="bg-white rounded-lg p-6 border">
            <h3 className="text-lg font-semibold mb-4" style={{ fontFamily: '"Dream Avenue"', color: '#3D0811' }}>
              Customer Acquisition Trend
            </h3>
            <ResponsiveContainer width="100%" height={300}>
              <BarChart data={customerData.acquisitionTrend}>
                <CartesianGrid strokeDasharray="3 3" />
                <XAxis dataKey="label" tick={{ fontSize: 12 }} />
                <YAxis tick={{ fontSize: 12 }} />
                <Tooltip />
                <Bar dataKey="newCustomers" fill="#3D0811" radius={[4, 4, 0, 0]} />
              </BarChart>
            </ResponsiveContainer>
          </div>

          {/* Top Customers & Geographic */}
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
            <div className="bg-white rounded-lg p-6 border">
              <h3 className="text-lg font-semibold mb-4" style={{ fontFamily: '"Dream Avenue"', color: '#3D0811' }}>
                Top Customers
              </h3>
              <div className="space-y-3">
                {customerData.topCustomers?.slice(0, 5).map((customer: TopCustomerData, index: number) => (
                  <div key={`customer-${customer.customerId}-${customer.email || index}`} className="flex items-center justify-between p-3 bg-gray-50 rounded-lg">
                    <div className="flex items-center gap-3">
                      <span className="w-8 h-8 rounded-full flex items-center justify-center text-white text-sm" style={{ backgroundColor: COLORS[index] }}>
                        {customer.customerName.charAt(0)}
                      </span>
                      <div>
                        <p className="font-medium text-sm">{customer.customerName}</p>
                        <p className="text-xs text-muted-foreground">{customer.orderCount} orders</p>
                      </div>
                    </div>
                    <p className="font-semibold text-sm" style={{ color: '#3D0811' }}>{formatCurrency(customer.totalSpent)}</p>
                  </div>
                ))}
              </div>
            </div>

            <div className="bg-white rounded-lg p-6 border">
              <h3 className="text-lg font-semibold mb-4" style={{ fontFamily: '"Dream Avenue"', color: '#3D0811' }}>
                Geographic Distribution
              </h3>
              <div className="space-y-3">
                {customerData.geographicDistribution?.slice(0, 5).map((geo: any, index: number) => (
                  <div key={geo.region} className="flex items-center justify-between">
                    <div className="flex items-center gap-3">
                      <span className="text-sm font-medium">{geo.region}</span>
                    </div>
                    <div className="flex items-center gap-2">
                      <div className="w-24 h-2 bg-gray-200 rounded-full overflow-hidden">
                        <div 
                          className="h-full rounded-full" 
                          style={{ width: `${geo.percentageOfTotal}%`, backgroundColor: COLORS[index] }}
                        />
                      </div>
                      <span className="text-sm text-muted-foreground">{geo.customerCount}</span>
                    </div>
                  </div>
                ))}
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Inventory Tab */}
      {!loading && activeTab === 'inventory' && inventoryData && (
        <div className="space-y-6">
          {/* Summary Cards */}
          <div className="grid grid-cols-1 md:grid-cols-5 gap-4">
            <div className="bg-white rounded-lg p-6 border">
              <p className="text-sm text-muted-foreground" style={{ fontFamily: '"Dream Avenue"' }}>Total Products</p>
              <p className="text-2xl font-semibold" style={{ color: '#3D0811' }}>{inventoryData.totalProducts}</p>
            </div>
            <div className="bg-white rounded-lg p-6 border">
              <p className="text-sm text-muted-foreground" style={{ fontFamily: '"Dream Avenue"' }}>Total Variants</p>
              <p className="text-2xl font-semibold" style={{ color: '#3D0811' }}>{inventoryData.totalVariants}</p>
            </div>
            <div className="bg-white rounded-lg p-6 border">
              <p className="text-sm text-muted-foreground" style={{ fontFamily: '"Dream Avenue"' }}>Inventory Value</p>
              <p className="text-2xl font-semibold" style={{ color: '#3D0811' }}>{formatCurrency(inventoryData.totalInventoryValue)}</p>
            </div>
            <div className="bg-white rounded-lg p-6 border">
              <p className="text-sm text-muted-foreground" style={{ fontFamily: '"Dream Avenue"' }}>Low Stock</p>
              <p className="text-2xl font-semibold text-orange-500">{inventoryData.lowStockCount}</p>
            </div>
            <div className="bg-white rounded-lg p-6 border">
              <p className="text-sm text-muted-foreground" style={{ fontFamily: '"Dream Avenue"' }}>Out of Stock</p>
              <p className="text-2xl font-semibold text-red-500">{inventoryData.outOfStockCount}</p>
            </div>
          </div>

          {/* Stock Forecast & Dead Stock */}
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
            <div className="bg-white rounded-lg p-6 border">
              <h3 className="text-lg font-semibold mb-4 flex items-center gap-2" style={{ fontFamily: '"Dream Avenue"', color: '#3D0811' }}>
                <AlertTriangle className="h-5 w-5 text-orange-500" />
                Stock Forecast (Risk of Stockout)
              </h3>
              <div className="space-y-3">
                {inventoryData.stockForecast?.slice(0, 5).map((item: StockForecastData) => (
                  <div key={item.productId} className="flex items-center justify-between p-3 bg-gray-50 rounded-lg">
                    <div>
                      <p className="font-medium text-sm">{item.productName}</p>
                      <p className="text-xs text-muted-foreground">Current: {item.currentStock} | Daily Sales: {item.averageDailySales.toFixed(1)}</p>
                    </div>
                    <div className="text-right">
                      <span className={`px-2 py-1 rounded text-xs ${
                        item.riskLevel === 'Critical' ? 'bg-red-100 text-red-800' :
                        item.riskLevel === 'High' ? 'bg-orange-100 text-orange-800' :
                        item.riskLevel === 'Medium' ? 'bg-yellow-100 text-yellow-800' :
                        'bg-green-100 text-green-800'
                      }`}>
                        {item.daysUntilStockout} days
                      </span>
                    </div>
                  </div>
                ))}
              </div>
            </div>

            <div className="bg-white rounded-lg p-6 border">
              <h3 className="text-lg font-semibold mb-4 flex items-center gap-2" style={{ fontFamily: '"Dream Avenue"', color: '#3D0811' }}>
                <Package className="h-5 w-5 text-gray-500" />
                Dead Stock (No Sales in 30+ Days)
              </h3>
              <div className="space-y-3">
                {inventoryData.deadStock?.slice(0, 5).map((item: DeadStockData) => (
                  <div key={item.productId} className="flex items-center justify-between p-3 bg-gray-50 rounded-lg">
                    <div>
                      <p className="font-medium text-sm">{item.productName}</p>
                      <p className="text-xs text-muted-foreground">Stock: {item.currentStock} | Value: {formatCurrency(item.stockValue)}</p>
                    </div>
                    <div className="text-right">
                      <p className="text-sm text-red-500">{item.daysWithoutSale} days</p>
                    </div>
                  </div>
                ))}
              </div>
            </div>
          </div>

          {/* Variant Performance */}
          <div className="bg-white rounded-lg p-6 border">
            <h3 className="text-lg font-semibold mb-4" style={{ fontFamily: '"Dream Avenue"', color: '#3D0811' }}>
              Variant Performance
            </h3>
            <ResponsiveContainer width="100%" height={300}>
              <BarChart data={inventoryData.variantPerformance?.slice(0, 10) || []}>
                <CartesianGrid strokeDasharray="3 3" />
                <XAxis dataKey="variantAttributes" tick={{ fontSize: 10 }} angle={-45} textAnchor="end" height={80} />
                <YAxis tick={{ fontSize: 12 }} />
                <Tooltip formatter={(value: number) => formatCurrency(value)} />
                <Bar dataKey="revenue" fill="#3D0811" radius={[4, 4, 0, 0]} />
              </BarChart>
            </ResponsiveContainer>
          </div>
        </div>
      )}

      {/* Financial Tab */}
      {!loading && activeTab === 'financial' && financialData && (
        <div className="space-y-6">
          {/* Summary Cards */}
          <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
            <div className="bg-white rounded-lg p-6 border">
              <p className="text-sm text-muted-foreground" style={{ fontFamily: '"Dream Avenue"' }}>Gross Revenue</p>
              <p className="text-2xl font-semibold text-green-600">{formatCurrency(financialData.grossRevenue)}</p>
            </div>
            <div className="bg-white rounded-lg p-6 border">
              <p className="text-sm text-muted-foreground" style={{ fontFamily: '"Dream Avenue"' }}>Total Costs</p>
              <p className="text-2xl font-semibold text-red-500">{formatCurrency(financialData.totalCosts)}</p>
            </div>
            <div className="bg-white rounded-lg p-6 border">
              <p className="text-sm text-muted-foreground" style={{ fontFamily: '"Dream Avenue"' }}>Gross Profit</p>
              <p className="text-2xl font-semibold" style={{ color: '#3D0811' }}>{formatCurrency(financialData.grossProfit)}</p>
            </div>
            <div className="bg-white rounded-lg p-6 border">
              <p className="text-sm text-muted-foreground" style={{ fontFamily: '"Dream Avenue"' }}>Gross Margin</p>
              <p className="text-2xl font-semibold" style={{ color: '#3D0811' }}>{financialData.grossMarginPercentage.toFixed(1)}%</p>
            </div>
          </div>

          {/* Monthly Trend */}
          <div className="bg-white rounded-lg p-6 border">
            <h3 className="text-lg font-semibold mb-4" style={{ fontFamily: '"Dream Avenue"', color: '#3D0811' }}>
              Monthly Financial Trend
            </h3>
            <ResponsiveContainer width="100%" height={300}>
              <LineChart data={financialData.monthlyTrend}>
                <CartesianGrid strokeDasharray="3 3" />
                <XAxis dataKey="monthName" tick={{ fontSize: 12 }} />
                <YAxis tick={{ fontSize: 12 }} />
                <Tooltip formatter={(value: number) => formatCurrency(value)} />
                <Legend />
                <Line type="monotone" dataKey="revenue" name="Revenue" stroke="#2E7D32" strokeWidth={2} />
                <Line type="monotone" dataKey="costs" name="Costs" stroke="#D32F2F" strokeWidth={2} />
                <Line type="monotone" dataKey="profit" name="Profit" stroke="#3D0811" strokeWidth={2} />
              </LineChart>
            </ResponsiveContainer>
          </div>

          {/* Payment Status & Product Margins */}
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
            <div className="bg-white rounded-lg p-6 border">
              <h3 className="text-lg font-semibold mb-4" style={{ fontFamily: '"Dream Avenue"', color: '#3D0811' }}>
                Payment Status
              </h3>
              <div className="space-y-4">
                <div className="flex items-center justify-between">
                  <span className="text-sm">Paid</span>
                  <div className="flex items-center gap-2">
                    <span className="text-sm font-medium text-green-600">{formatCurrency(financialData.paymentStatus.paidAmount)}</span>
                    <span className="text-xs text-muted-foreground">({financialData.paymentStatus.paidOrders} orders)</span>
                  </div>
                </div>
                <div className="flex items-center justify-between">
                  <span className="text-sm">Pending</span>
                  <div className="flex items-center gap-2">
                    <span className="text-sm font-medium text-yellow-600">{formatCurrency(financialData.paymentStatus.pendingAmount)}</span>
                    <span className="text-xs text-muted-foreground">({financialData.paymentStatus.pendingOrders} orders)</span>
                  </div>
                </div>
                <div className="flex items-center justify-between">
                  <span className="text-sm">Partially Paid</span>
                  <div className="flex items-center gap-2">
                    <span className="text-sm font-medium text-orange-600">{formatCurrency(financialData.paymentStatus.partiallyPaidAmount)}</span>
                    <span className="text-xs text-muted-foreground">({financialData.paymentStatus.partiallyPaidOrders} orders)</span>
                  </div>
                </div>
                <hr />
                <div className="pt-2">
                  <h4 className="font-medium mb-2">Down Payments</h4>
                  <p className="text-sm text-muted-foreground">Total: {formatCurrency(financialData.downPayments.totalDownPayments)}</p>
                  <p className="text-sm text-muted-foreground">Outstanding: {formatCurrency(financialData.downPayments.outstandingBalance)}</p>
                </div>
              </div>
            </div>

            <div className="bg-white rounded-lg p-6 border">
              <h3 className="text-lg font-semibold mb-4" style={{ fontFamily: '"Dream Avenue"', color: '#3D0811' }}>
                Top Products by Profit
              </h3>
              <div className="space-y-3">
                {financialData.productMargins?.slice(0, 5).map((product: ProductMarginData, index: number) => (
                  <div key={product.productId} className="flex items-center justify-between p-3 bg-gray-50 rounded-lg">
                    <div>
                      <p className="font-medium text-sm">{product.productName}</p>
                      <p className="text-xs text-muted-foreground">Margin: {product.grossMarginPercentage.toFixed(1)}% | Qty: {product.quantitySold}</p>
                    </div>
                    <p className="font-semibold text-sm text-green-600">{formatCurrency(product.totalProfit)}</p>
                  </div>
                ))}
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Promo Codes Tab */}
      {!loading && activeTab === 'promo' && promoData && (
        <div className="space-y-6">
          {/* Summary Cards */}
          <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
            <div className="bg-white rounded-lg p-6 border">
              <p className="text-sm text-muted-foreground" style={{ fontFamily: '"Dream Avenue"' }}>Total Promo Codes</p>
              <p className="text-2xl font-semibold" style={{ color: '#3D0811' }}>{promoData.totalPromoCodes}</p>
              <p className="text-xs text-muted-foreground">{promoData.activePromoCodes} active</p>
            </div>
            <div className="bg-white rounded-lg p-6 border">
              <p className="text-sm text-muted-foreground" style={{ fontFamily: '"Dream Avenue"' }}>Total Discounts Given</p>
              <p className="text-2xl font-semibold text-red-500">{formatCurrency(promoData.totalDiscountsGiven)}</p>
            </div>
            <div className="bg-white rounded-lg p-6 border">
              <p className="text-sm text-muted-foreground" style={{ fontFamily: '"Dream Avenue"' }}>Total Usage</p>
              <p className="text-2xl font-semibold" style={{ color: '#3D0811' }}>{promoData.totalUsageCount}</p>
            </div>
            <div className="bg-white rounded-lg p-6 border">
              <p className="text-sm text-muted-foreground" style={{ fontFamily: '"Dream Avenue"' }}>Avg Discount/Use</p>
              <p className="text-2xl font-semibold" style={{ color: '#3D0811' }}>{formatCurrency(promoData.averageDiscountPerUse)}</p>
            </div>
          </div>

          {/* Usage Trend */}
          <div className="bg-white rounded-lg p-6 border">
            <h3 className="text-lg font-semibold mb-4" style={{ fontFamily: '"Dream Avenue"', color: '#3D0811' }}>
              Promo Code Usage (Last 30 Days)
            </h3>
            <ResponsiveContainer width="100%" height={300}>
              <AreaChart data={promoData.usageTrend}>
                <defs>
                  <linearGradient id="colorUsage" x1="0" y1="0" x2="0" y2="1">
                    <stop offset="5%" stopColor="#7B1FA2" stopOpacity={0.8}/>
                    <stop offset="95%" stopColor="#7B1FA2" stopOpacity={0}/>
                  </linearGradient>
                </defs>
                <CartesianGrid strokeDasharray="3 3" />
                <XAxis dataKey="label" tick={{ fontSize: 10 }} angle={-45} textAnchor="end" height={60} />
                <YAxis tick={{ fontSize: 12 }} />
                <Tooltip />
                <Area type="monotone" dataKey="usageCount" stroke="#7B1FA2" fillOpacity={1} fill="url(#colorUsage)" />
              </AreaChart>
            </ResponsiveContainer>
          </div>

          {/* Top Promo Codes */}
          <div className="bg-white rounded-lg p-6 border">
            <h3 className="text-lg font-semibold mb-4" style={{ fontFamily: '"Dream Avenue"', color: '#3D0811' }}>
              Promo Code Performance
            </h3>
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b">
                    <th className="text-left py-2">Code</th>
                    <th className="text-left py-2">Type</th>
                    <th className="text-right py-2">Usage</th>
                    <th className="text-right py-2">Discounts</th>
                    <th className="text-right py-2">Revenue</th>
                    <th className="text-right py-2">ROI</th>
                    <th className="text-center py-2">Status</th>
                  </tr>
                </thead>
                <tbody>
                  {promoData.topPromoCodes?.map((promo: PromoCodePerformanceData) => (
                    <tr key={promo.promoCodeId} className="border-b">
                      <td className="py-2 font-medium">{promo.code}</td>
                      <td className="py-2">{promo.discountType} ({promo.discountValue}{promo.discountType === 'Percentage' ? '%' : ' EGP'})</td>
                      <td className="py-2 text-right">{promo.usageCount}{promo.usageLimit > 0 ? `/${promo.usageLimit}` : ''}</td>
                      <td className="py-2 text-right text-red-500">{formatCurrency(promo.totalDiscountGiven)}</td>
                      <td className="py-2 text-right text-green-600">{formatCurrency(promo.revenueGenerated)}</td>
                      <td className="py-2 text-right">{promo.roi.toFixed(1)}%</td>
                      <td className="py-2 text-center">
                        <span className={`px-2 py-1 rounded text-xs ${promo.isActive ? 'bg-green-100 text-green-800' : 'bg-gray-100 text-gray-800'}`}>
                          {promo.isActive ? 'Active' : 'Inactive'}
                        </span>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        </div>
      )}

      {/* Orders Tab */}
      {!loading && activeTab === 'orders' && orderStatusData && (
        <div className="space-y-6">
          {/* Summary Cards */}
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <div className="bg-white rounded-lg p-6 border">
              <p className="text-sm text-muted-foreground" style={{ fontFamily: '"Dream Avenue"' }}>Total Orders</p>
              <p className="text-2xl font-semibold" style={{ color: '#3D0811' }}>{orderStatusData.totalOrders}</p>
            </div>
            <div className="bg-white rounded-lg p-6 border">
              <div className="flex items-center gap-2">
                <Clock className="h-5 w-5 text-blue-500" />
                <div>
                  <p className="text-sm text-muted-foreground" style={{ fontFamily: '"Dream Avenue"' }}>Avg Fulfillment Time</p>
                  <p className="text-2xl font-semibold" style={{ color: '#3D0811' }}>{orderStatusData.averageFulfillmentTimeHours.toFixed(1)} hrs</p>
                </div>
              </div>
            </div>
            <div className="bg-white rounded-lg p-6 border">
              <p className="text-sm text-muted-foreground" style={{ fontFamily: '"Dream Avenue"' }}>Status Breakdown</p>
              <p className="text-2xl font-semibold" style={{ color: '#3D0811' }}>{orderStatusData.statusDistribution?.length || 0} statuses</p>
            </div>
          </div>

          {/* Status Distribution Pie & Bar */}
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
            <div className="bg-white rounded-lg p-6 border">
              <h3 className="text-lg font-semibold mb-4" style={{ fontFamily: '"Dream Avenue"', color: '#3D0811' }}>
                Order Status Distribution
              </h3>
              <ResponsiveContainer width="100%" height={300}>
                <PieChart>
                  <Pie
                    data={orderStatusData.statusDistribution}
                    cx="50%"
                    cy="50%"
                    labelLine={false}
                    label={({ status, percentage }) => `${status}: ${percentage.toFixed(1)}%`}
                    outerRadius={100}
                    fill="#8884d8"
                    dataKey="count"
                    nameKey="status"
                  >
                    {orderStatusData.statusDistribution?.map((entry: any, index: number) => (
                      <Cell key={`cell-${index}`} fill={
                        entry.status === 'Delivered' ? '#2E7D32' :
                        entry.status === 'Pending' ? '#FFA000' :
                        entry.status === 'Cancelled' ? '#D32F2F' :
                        entry.status === 'Shipped' ? '#1976D2' :
                        COLORS[index % COLORS.length]
                      } />
                    ))}
                  </Pie>
                  <Tooltip />
                </PieChart>
              </ResponsiveContainer>
            </div>

            <div className="bg-white rounded-lg p-6 border">
              <h3 className="text-lg font-semibold mb-4" style={{ fontFamily: '"Dream Avenue"', color: '#3D0811' }}>
                Status by Value
              </h3>
              <div className="space-y-4">
                {orderStatusData.statusDistribution?.map((status: OrderStatusData, index: number) => (
                  <div key={status.status} className="flex items-center justify-between">
                    <div className="flex items-center gap-3">
                      <div className="w-3 h-3 rounded-full" style={{ backgroundColor: 
                        status.status === 'Delivered' ? '#2E7D32' :
                        status.status === 'Pending' ? '#FFA000' :
                        status.status === 'Cancelled' ? '#D32F2F' :
                        status.status === 'Shipped' ? '#1976D2' :
                        COLORS[index % COLORS.length]
                      }} />
                      <span className="text-sm font-medium">{status.status}</span>
                    </div>
                    <div className="text-right">
                      <p className="text-sm font-semibold">{formatCurrency(status.totalValue)}</p>
                      <p className="text-xs text-muted-foreground">{status.count} orders</p>
                    </div>
                  </div>
                ))}
              </div>
            </div>
          </div>

          {/* Status Trend */}
          <div className="bg-white rounded-lg p-6 border">
            <h3 className="text-lg font-semibold mb-4" style={{ fontFamily: '"Dream Avenue"', color: '#3D0811' }}>
              Order Status Trend (Last 30 Days)
            </h3>
            <ResponsiveContainer width="100%" height={300}>
              <AreaChart data={orderStatusData.statusTrend}>
                <CartesianGrid strokeDasharray="3 3" />
                <XAxis dataKey="label" tick={{ fontSize: 10 }} angle={-45} textAnchor="end" height={60} />
                <YAxis tick={{ fontSize: 12 }} />
                <Tooltip />
                <Legend />
                <Area type="monotone" dataKey="delivered" name="Delivered" stackId="1" stroke="#2E7D32" fill="#2E7D32" />
                <Area type="monotone" dataKey="shipped" name="Shipped" stackId="1" stroke="#1976D2" fill="#1976D2" />
                <Area type="monotone" dataKey="confirmed" name="Confirmed" stackId="1" stroke="#7B1FA2" fill="#7B1FA2" />
                <Area type="monotone" dataKey="pending" name="Pending" stackId="1" stroke="#FFA000" fill="#FFA000" />
                <Area type="monotone" dataKey="cancelled" name="Cancelled" stackId="1" stroke="#D32F2F" fill="#D32F2F" />
              </AreaChart>
            </ResponsiveContainer>
          </div>
        </div>
      )}
    </div>
  )
}
