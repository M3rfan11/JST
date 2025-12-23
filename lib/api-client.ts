/**
 * API Client for .NET Backend
 * 
 * This utility handles all API calls to your .NET backend.
 * Update the API_URL in your .env.local file.
 */

const API_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:8081'

interface ApiOptions extends RequestInit {
  params?: Record<string, string | number | boolean>
}

class ApiClient {
  private baseUrl: string

  constructor(baseUrl: string) {
    this.baseUrl = baseUrl
  }

  private async request<T>(
    endpoint: string,
    options: ApiOptions = {}
  ): Promise<T> {
    const { params, ...fetchOptions } = options

    // Build URL with query parameters
    let url = `${this.baseUrl}${endpoint}`
    if (params) {
      const searchParams = new URLSearchParams()
      Object.entries(params).forEach(([key, value]) => {
        searchParams.append(key, String(value))
      })
      url += `?${searchParams.toString()}`
    }

    // Set default headers
    const headers: HeadersInit = {
      'Content-Type': 'application/json',
      ...fetchOptions.headers,
    }

    // Add auth token if available
    if (typeof window !== 'undefined') {
      const token = localStorage.getItem('authToken')
      if (token) {
        headers['Authorization'] = `Bearer ${token}`
      }
    }

    try {
      const response = await fetch(url, {
        ...fetchOptions,
        headers,
      })

      if (!response.ok) {
        let errorData: any
        let errorMessage: string = ''
        
        // Read response as text first, then try to parse as JSON
        const contentType = response.headers.get('content-type')
        const responseText = await response.text()
        
        try {
          // Try to parse as JSON
          if (contentType && contentType.includes('application/json')) {
            errorData = JSON.parse(responseText)
          } else {
            // If not JSON content type, try parsing anyway (some APIs return JSON without proper header)
            try {
              errorData = JSON.parse(responseText)
            } catch {
              // It's plain text, use it as the message
              errorMessage = responseText.trim()
              errorData = { message: errorMessage }
            }
          }
        } catch (e) {
          // If parsing fails, treat as plain text
          errorMessage = responseText.trim()
          errorData = { message: errorMessage || `HTTP error! status: ${response.status}` }
        }
        
        // Extract error message from various possible formats
        errorMessage = errorMessage || 
                      errorData.message || 
                      errorData.error || 
                      errorData.title ||
                      errorData.detail ||
                      (typeof errorData === 'string' ? errorData : null) ||
                      `API Error: ${response.status}`
        const error = new Error(errorMessage)
        ;(error as any).status = response.status
        ;(error as any).data = errorData
        throw error
      }

      // Handle empty responses
      const contentType = response.headers.get('content-type')
      if (contentType && contentType.includes('application/json')) {
        return await response.json()
      }
      return {} as T
    } catch (error) {
      console.error('API request failed:', error)
      throw error
    }
  }

  // GET request
  async get<T>(endpoint: string, params?: Record<string, string | number | boolean>): Promise<T> {
    return this.request<T>(endpoint, { method: 'GET', params })
  }

  // POST request
  async post<T>(endpoint: string, data?: unknown): Promise<T> {
    return this.request<T>(endpoint, {
      method: 'POST',
      body: JSON.stringify(data),
    })
  }

  // PUT request
  async put<T>(endpoint: string, data?: unknown): Promise<T> {
    return this.request<T>(endpoint, {
      method: 'PUT',
      body: JSON.stringify(data),
    })
  }

  // PATCH request
  async patch<T>(endpoint: string, data?: unknown): Promise<T> {
    return this.request<T>(endpoint, {
      method: 'PATCH',
      body: JSON.stringify(data),
    })
  }

  // DELETE request
  async delete<T>(endpoint: string): Promise<T> {
    return this.request<T>(endpoint, { method: 'DELETE' })
  }
}

// Create singleton instance
export const apiClient = new ApiClient(API_URL)

// Convenience functions for common endpoints
export const api = {
  // Products
  products: {
    getAll: () => apiClient.get('/api/product/all'), // Admin endpoint with all products (requires auth)
    getPublic: (params?: { categoryName?: string }) => apiClient.get('/api/product', params), // Public endpoint (active only, optional category filter)
    getById: (id: string) => apiClient.get(`/api/product/${id}`),
    create: (data: unknown) => apiClient.post('/api/product', data),
    update: (id: string, data: unknown) => apiClient.put(`/api/product/${id}`, data),
    delete: (id: string) => apiClient.delete(`/api/product/${id}`),
  },

  // Orders
  orders: {
    getAll: () => apiClient.get('/api/orders'),
    getById: (id: string) => apiClient.get(`/api/orders/${id}`),
    create: (data: unknown) => apiClient.post('/api/orders', data),
    update: (id: string, data: unknown) => apiClient.put(`/api/orders/${id}`, data),
    track: (orderId: string) => apiClient.get(`/api/orders/${orderId}/track`),
  },

  // Auth
  auth: {
    login: (email: string, password: string) =>
      apiClient.post('/api/auth/login', { email, password }),
    signup: (data: unknown) => apiClient.post('/api/auth/register', data),
    logout: () => apiClient.post('/api/auth/logout'),
    me: () => apiClient.get('/api/auth/me'),
  },

  // Users
  users: {
    getProfile: () => apiClient.get('/api/users/profile'),
    updateProfile: (data: unknown) => apiClient.put('/api/users/profile', data),
    getAddresses: () => apiClient.get('/api/users/addresses'),
    addAddress: (data: unknown) => apiClient.post('/api/users/addresses', data),
    updateAddress: (id: string, data: unknown) =>
      apiClient.put(`/api/users/addresses/${id}`, data),
    deleteAddress: (id: string) => apiClient.delete(`/api/users/addresses/${id}`),
  },

  // Cart
  cart: {
    get: () => apiClient.get('/api/customerorder/cart'),
    addItem: (data: unknown) => apiClient.post('/api/customerorder/cart/add', data),
    updateItem: (itemId: number, data: unknown) =>
      apiClient.put(`/api/customerorder/cart/${itemId}`, data),
    removeItem: (itemId: number) => apiClient.delete(`/api/customerorder/cart/${itemId}`),
    clear: () => apiClient.delete('/api/customerorder/cart/clear'),
  },

  // Promo codes / Coupons
  coupons: {
    validate: (code: string, orderAmount: number, productIds?: number[], userId?: number) => 
      apiClient.post('/api/promocode/validate', { 
        code, 
        orderAmount, 
        productIds: productIds || [],
        userId: userId || null
      }),
    apply: (code: string) => apiClient.post('/api/coupons/apply', { code }),
  },

  // Dashboard
  dashboard: {
    getStats: () => apiClient.get('/api/dashboard/stats'),
    getRecentActivity: () => apiClient.get('/api/dashboard/recent-activity'),
    getLowStockItems: () => apiClient.get('/api/dashboard/low-stock-items'),
    refreshTotals: () => apiClient.post('/api/dashboard/refresh-totals'),
  },

  // Admin
  admin: {
    getUsers: () => apiClient.get('/api/admin/users'),
    updateUser: (id: number, data: unknown) => apiClient.put(`/api/admin/users/${id}`, data),
    deleteUser: (id: number) => apiClient.delete(`/api/admin/users/${id}`),
  },

  // Categories
  categories: {
    getAll: () => apiClient.get('/api/category/all'), // Admin endpoint - returns all categories including inactive
    getPublic: () => apiClient.get('/api/category'), // Public endpoint - returns only active categories
    getById: (id: number) => apiClient.get(`/api/category/${id}`),
    create: (data: unknown) => apiClient.post('/api/category', data),
    update: (id: number, data: unknown) => apiClient.put(`/api/category/${id}`, data),
    delete: (id: number) => apiClient.delete(`/api/category/${id}`),
  },

  // Promo Codes (Admin)
  promoCodes: {
    getAll: () => apiClient.get('/api/promocode'), // Backend route: api/[controller] = api/PromoCode (case-insensitive)
    getById: (id: number) => apiClient.get(`/api/promocode/${id}`),
    create: (data: unknown) => apiClient.post('/api/promocode', data),
    update: (id: number, data: unknown) => apiClient.put(`/api/promocode/${id}`, data),
    delete: (id: number) => apiClient.delete(`/api/promocode/${id}`),
    getCustomers: () => apiClient.get('/api/promocode/customers'), // Get customers for promo code assignment
  },

  // Online Orders
  onlineOrders: {
    getAll: () => apiClient.get('/api/onlineorder'), // Backend route: api/[controller] = api/OnlineOrder (ASP.NET Core routing is case-insensitive)
    getById: (id: number) => apiClient.get(`/api/onlineorder/${id}`),
    updateStatus: (id: number, status: string) => apiClient.put(`/api/onlineorder/${id}/status`, { status }),
  },

  // Customer Orders
  customerOrders: {
    createGuestOrder: (data: unknown) => apiClient.post('/api/customerorder/guest-order', data),
    createOrder: (data: unknown) => apiClient.post('/api/customerorder/order', data),
    getOrders: () => apiClient.get('/api/customerorder/orders'),
    getOrderById: (id: number) => apiClient.get(`/api/customerorder/order/${id}`),
    trackOrderByNumber: (orderNumber: string, email?: string) => {
      // If email is provided, use POST endpoint (for guest users)
      // Otherwise, use GET endpoint (for authenticated users)
      if (email) {
        return apiClient.post('/api/customerorder/track-order', { orderNumber, email })
      } else {
        return apiClient.get(`/api/customerorder/track/${orderNumber}`)
      }
    },
  },

  // Product Inventory
  productInventory: {
    getAll: () => apiClient.get('/api/productinventory'),
    getById: (id: number) => apiClient.get(`/api/productinventory/${id}`),
    create: (data: unknown) => apiClient.post('/api/productinventory', data),
    update: (id: number, data: unknown) => apiClient.put(`/api/productinventory/${id}`, data),
    delete: (id: number) => apiClient.delete(`/api/productinventory/${id}`),
  },

  // Variant Inventory
  variantInventory: {
    getByVariant: (variantId: number) => apiClient.get(`/api/variantinventory/variant/${variantId}`),
    getBatch: (variantIds: number[]) => apiClient.post('/api/variantinventory/get-batch', variantIds),
    create: (data: unknown) => apiClient.post('/api/variantinventory', data),
    update: (id: number, data: unknown) => apiClient.put(`/api/variantinventory/${id}`, data),
    delete: (id: number) => apiClient.delete(`/api/variantinventory/${id}`),
  },

  // Product Variants
  productVariants: {
    getByProduct: (productId: number) => apiClient.get(`/api/productvariant/product/${productId}`),
    getById: (id: number) => apiClient.get(`/api/productvariant/${id}`),
    create: (data: any) => {
      const { productId, ...variantData } = data
      return apiClient.post(`/api/productvariant?productId=${productId}`, variantData)
    },
    update: (id: number, data: unknown) => apiClient.put(`/api/productvariant/${id}`, data),
    delete: (id: number) => apiClient.delete(`/api/productvariant/${id}`),
  },

  // Settings
  settings: {
    getShippingTicker: () => apiClient.get('/api/settings/shipping-ticker'),
    updateShippingTicker: (messages: string[]) => apiClient.put('/api/settings/shipping-ticker', { messages }),
    getInstaPayQr: () => apiClient.get('/api/settings/instapay-qr'),
    uploadInstaPayQr: (formData: FormData) => {
      return fetch(`${API_URL}/api/settings/instapay-qr`, {
        method: 'POST',
        body: formData,
        headers: {
          ...(typeof window !== 'undefined' && localStorage.getItem('authToken')
            ? { Authorization: `Bearer ${localStorage.getItem('authToken')}` }
            : {}),
        },
      }).then(async (response) => {
        if (!response.ok) {
          const errorData = await response.json().catch(() => ({ message: 'Upload failed' }))
          const error = new Error(errorData.message || 'Upload failed')
          ;(error as any).status = response.status
          throw error
        }
        return response.json()
      })
    },
    deleteInstaPayQr: () => apiClient.delete('/api/settings/instapay-qr'),
  },

  // InstaPay
  instapay: {
    uploadProof: (orderId: number, formData: FormData) => {
      // For file uploads, we need to use fetch directly
      return fetch(`${API_URL}/api/instapay/orders/${orderId}/proof`, {
        method: 'POST',
        body: formData,
        headers: {
          // Don't set Content-Type - let browser set it with boundary for FormData
          ...(typeof window !== 'undefined' && localStorage.getItem('authToken')
            ? { Authorization: `Bearer ${localStorage.getItem('authToken')}` }
            : {}),
        },
      }).then(async (response) => {
        if (!response.ok) {
          const errorData = await response.json().catch(() => ({ message: 'Upload failed' }))
          const error = new Error(errorData.message || 'Upload failed')
          ;(error as any).status = response.status
          throw error
        }
        return response.json()
      })
    },
    getOrder: (orderId: number) => apiClient.get(`/api/instapay/orders/${orderId}`),
    getPendingProofs: () => apiClient.get('/api/instapay/admin/pending-proofs'),
    acceptPayment: (orderId: number, data?: { adminNote?: string }) => 
      apiClient.post(`/api/instapay/admin/orders/${orderId}/accept`, data || {}),
    rejectPayment: (orderId: number, data: { rejectionReason: string; adminNote?: string }) => 
      apiClient.post(`/api/instapay/admin/orders/${orderId}/reject`, data),
  },

  // Reports & Analytics
  reports: {
    // Sales Reports
    getSalesReport: (params?: { year?: number; quarter?: number; storeId?: number }) => 
      apiClient.get('/api/reports/sales', params),
    
    // Purchase Reports  
    getPurchaseReport: (params?: { year?: number; quarter?: number; storeId?: number }) => 
      apiClient.get('/api/reports/purchases', params),
    
    // Peak Sales Analysis
    getPeakSales: (params?: { year?: number; quarter?: number; storeId?: number }) => 
      apiClient.get('/api/reports/peak-sales', params),
    
    // Revenue Over Time
    getRevenueOverTime: (params?: { period?: string; startDate?: string; endDate?: string }) => 
      apiClient.get('/api/reports/revenue-over-time', params),
    
    // Category Sales
    getCategorySales: (params?: { year?: number; month?: number }) => 
      apiClient.get('/api/reports/category-sales', params),
    
    // Customer Analytics
    getCustomerAnalytics: () => 
      apiClient.get('/api/reports/customer-analytics'),
    
    // Inventory Reports
    getInventoryReport: (params?: { deadStockDays?: number }) => 
      apiClient.get('/api/reports/inventory', params),
    
    // Financial Reports
    getFinancialReport: (params?: { year?: number; month?: number }) => 
      apiClient.get('/api/reports/financial', params),
    
    // Promo Code Analytics
    getPromoAnalytics: () => 
      apiClient.get('/api/reports/promo-analytics'),
    
    // Order Status Distribution
    getOrderStatusDistribution: (params?: { year?: number; month?: number }) => 
      apiClient.get('/api/reports/order-status', params),
  },
}

export default apiClient

