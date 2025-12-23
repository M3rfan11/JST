"use client"

import { useState, useEffect } from "react"
import { useParams, useRouter } from "next/navigation"
import { Header } from "@/components/header"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { useToast } from "@/hooks/use-toast"
import { QrCode, Upload, CheckCircle, MessageCircle, ArrowLeft } from "lucide-react"
import { api } from "@/lib/api-client"
import Link from "next/link"
import Image from "next/image"

interface Order {
  id: number
  orderNumber: string
  totalAmount: number
  status: string
  paymentStatus: string
  customerName: string
  customerPhone?: string
}

export default function PaymentPendingPage() {
  const params = useParams()
  const router = useRouter()
  const { toast } = useToast()
  const orderId = parseInt(params.orderId as string)
  
  const [order, setOrder] = useState<Order | null>(null)
  const [loading, setLoading] = useState(true)
  const [uploading, setUploading] = useState(false)
  const [file, setFile] = useState<File | null>(null)
  const [proofSubmitted, setProofSubmitted] = useState(false)
  const [qrCodeUrl, setQrCodeUrl] = useState<string | null>(null)

  useEffect(() => {
    loadOrder()
    loadQrCode()
  }, [orderId])

  const loadQrCode = async () => {
    try {
      const response = await api.settings.getInstaPayQr() as { qrCodeUrl?: string | null }
      if (response && response.qrCodeUrl) {
        setQrCodeUrl(response.qrCodeUrl)
      }
    } catch (error) {
      console.error("Error loading QR code:", error)
      // QR code is optional, so we don't show error to user
    }
  }

  const loadOrder = async () => {
    try {
      setLoading(true)
      // Try InstaPay endpoint first, fallback to customer order endpoint
      let orderData: any
      try {
        orderData = await api.instapay.getOrder(orderId) as any
      } catch (error: any) {
        // If InstaPay endpoint not available (404), try customer order endpoint
        if (error.status === 404) {
          console.warn("InstaPay endpoint not available, using customer order endpoint")
          try {
            orderData = await api.customerOrders.getOrderById(orderId) as any
            // Check if it's an InstaPay order
            if (orderData.paymentMethod?.toUpperCase() !== "INSTAPAY") {
              throw new Error("This order is not an InstaPay order")
            }
          } catch (fallbackError: any) {
            throw error // Throw original error if fallback also fails
          }
        } else {
          throw error
        }
      }

      setOrder({
        id: orderData.id,
        orderNumber: orderData.orderNumber,
        totalAmount: orderData.totalAmount,
        status: orderData.status,
        paymentStatus: orderData.paymentStatus,
        customerName: orderData.customerName,
        customerPhone: orderData.customerPhone,
      })
      
      // Check if proof already submitted
      if (orderData.status === "PROOF_SUBMITTED" || orderData.status === "UNDER_REVIEW" || orderData.hasProof) {
        setProofSubmitted(true)
      }
    } catch (error: any) {
      console.error("Error loading order:", error)
      toast({
        title: "Error",
        description: error.message || "Failed to load order details. Please restart the backend server.",
        variant: "destructive",
      })
    } finally {
      setLoading(false)
    }
  }

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files && e.target.files[0]) {
      const selectedFile = e.target.files[0]
      // Validate file type
      if (!selectedFile.type.startsWith("image/") && selectedFile.type !== "application/pdf") {
        toast({
          title: "Invalid file type",
          description: "Please upload an image (JPG, PNG) or PDF file.",
          variant: "destructive",
        })
        return
      }
      // Validate file size (max 5MB)
      if (selectedFile.size > 5 * 1024 * 1024) {
        toast({
          title: "File too large",
          description: "Please upload a file smaller than 5MB.",
          variant: "destructive",
        })
        return
      }
      setFile(selectedFile)
    }
  }

  const handleSubmitProof = async () => {
    if (!file) {
      toast({
        title: "No file selected",
        description: "Please select a payment proof file to upload.",
        variant: "destructive",
      })
      return
    }

    try {
      setUploading(true)
      
      // Create FormData for file upload
      const formData = new FormData()
      formData.append("file", file)
      
      // Upload proof
      await api.instapay.uploadProof(orderId, formData)
      
      setProofSubmitted(true)
      toast({
        title: "Proof submitted successfully!",
        description: "Your payment proof has been submitted. We'll review and confirm shortly.",
      })
      
      // Reload order to get updated status
      await loadOrder()
    } catch (error: any) {
      console.error("Error uploading proof:", error)
      toast({
        title: "Upload failed",
        description: error.message || "Failed to upload payment proof. Please try again.",
        variant: "destructive",
      })
    } finally {
      setUploading(false)
    }
  }

  const handleWhatsAppContact = () => {
    if (!order) return
    const message = encodeURIComponent(
      `Hello, I need assistance with my order #${order.orderNumber}. Order ID: ${order.id}`
    )
    window.open(`https://wa.me/201234567890?text=${message}`, "_blank")
  }

  if (loading) {
    return (
      <div className="min-h-screen">
        <Header />
        <div className="container mx-auto px-4 py-16 text-center">
          <p style={{ fontFamily: '"Dream Avenue"' }}>Loading order details...</p>
        </div>
      </div>
    )
  }

  if (!order) {
    return (
      <div className="min-h-screen">
        <Header />
        <div className="container mx-auto px-4 py-16 text-center">
          <p style={{ fontFamily: '"Dream Avenue"' }}>Order not found</p>
          <Button asChild className="mt-4">
            <Link href="/track">Track Order</Link>
          </Button>
        </div>
      </div>
    )
  }

  return (
    <div className="min-h-screen bg-background">
      <Header />
      <div className="container mx-auto px-4 sm:px-6 py-6 sm:py-8 md:py-12">
        <Button variant="ghost" asChild className="mb-4" style={{ fontFamily: '"Dream Avenue"' }}>
          <Link href="/track">
            <ArrowLeft className="h-4 w-4 mr-2" />
            Back to Track Order
          </Link>
        </Button>

        <div className="max-w-2xl mx-auto">
          <h1 className="font-serif text-3xl sm:text-4xl font-semibold mb-2" style={{ fontFamily: '"Dream Avenue"', color: '#3D0811' }}>
            InstaPay Payment
          </h1>
          <p className="text-muted-foreground mb-6" style={{ fontFamily: '"Dream Avenue"' }}>
            Order #{order.orderNumber}
          </p>

          {proofSubmitted ? (
            <div className="bg-green-50 border border-green-200 rounded-lg p-6 mb-6">
              <div className="flex items-center gap-3 mb-4">
                <CheckCircle className="h-6 w-6 text-green-600" />
                <h2 className="text-lg font-semibold text-green-800" style={{ fontFamily: '"Dream Avenue"' }}>
                  Proof Submitted Successfully
                </h2>
              </div>
              <p className="text-green-700 mb-4" style={{ fontFamily: '"Dream Avenue"' }}>
                Your payment proof has been submitted. We'll review and confirm shortly. You'll receive a notification once your payment is confirmed.
              </p>
              <Button asChild variant="outline" style={{ fontFamily: '"Dream Avenue"' }}>
                <Link href={`/track?orderId=${order.orderNumber}`}>View Order Status</Link>
              </Button>
            </div>
          ) : (
            <>
              {/* Payment Instructions */}
              <div className="bg-white border border-border rounded-lg p-6 mb-6">
                <h2 className="font-serif text-xl font-semibold mb-4" style={{ fontFamily: '"Dream Avenue"', color: '#3D0811' }}>
                  Payment Instructions
                </h2>
                <ol className="space-y-3 list-decimal list-inside text-sm" style={{ fontFamily: '"Dream Avenue"' }}>
                  <li>Open your banking app and select InstaPay</li>
                  <li>Scan the QR code below or use the reference number</li>
                  <li>Enter the amount: <strong>{order.totalAmount.toFixed(2)} EGP</strong></li>
                  <li>Complete the payment</li>
                  <li>Upload a screenshot or receipt of the payment confirmation</li>
                </ol>
              </div>

              {/* QR Code Section */}
              <div className="bg-white border border-border rounded-lg p-6 mb-6 text-center">
                <h2 className="font-serif text-xl font-semibold mb-4" style={{ fontFamily: '"Dream Avenue"', color: '#3D0811' }}>
                  <QrCode className="h-5 w-5 inline mr-2" />
                  Scan QR Code
                </h2>
                <div className="bg-gray-100 rounded-lg p-4 mb-4 flex justify-center">
                  {qrCodeUrl ? (
                    <div className="w-64 h-64 bg-white rounded-lg p-4 flex items-center justify-center border border-gray-300">
                      <Image
                        src={`${process.env.NEXT_PUBLIC_API_URL || 'http://localhost:8081'}${qrCodeUrl}`}
                        alt="InstaPay QR Code"
                        width={256}
                        height={256}
                        className="object-contain"
                        unoptimized
                      />
                    </div>
                  ) : (
                    <div className="w-64 h-64 bg-white rounded-lg flex items-center justify-center border-2 border-dashed border-gray-300">
                      <div className="text-center">
                        <QrCode className="h-32 w-32 text-gray-400 mx-auto mb-2" />
                        <p className="text-xs text-gray-500" style={{ fontFamily: '"Dream Avenue"' }}>
                          QR code not configured
                        </p>
                      </div>
                    </div>
                  )}
                </div>
                <p className="text-sm text-muted-foreground mb-2" style={{ fontFamily: '"Dream Avenue"' }}>
                  Reference Number
                </p>
                <p className="text-lg font-mono font-semibold" style={{ fontFamily: '"Dream Avenue"', color: '#3D0811' }}>
                  {order.orderNumber}
                </p>
              </div>

              {/* Upload Proof Section */}
              <div className="bg-white border border-border rounded-lg p-6 mb-6">
                <h2 className="font-serif text-xl font-semibold mb-4" style={{ fontFamily: '"Dream Avenue"', color: '#3D0811' }}>
                  <Upload className="h-5 w-5 inline mr-2" />
                  Upload Payment Proof
                </h2>
                <div className="space-y-4">
                  <div>
                    <Label htmlFor="proofFile" className="text-sm" style={{ fontFamily: '"Dream Avenue"' }}>
                      Payment Receipt (Image or PDF)
                    </Label>
                    <Input
                      id="proofFile"
                      type="file"
                      accept="image/*,.pdf"
                      onChange={handleFileChange}
                      className="mt-1.5"
                      disabled={uploading}
                    />
                    <p className="text-xs text-muted-foreground mt-1" style={{ fontFamily: '"Dream Avenue"' }}>
                      Accepted formats: JPG, PNG, PDF (Max 5MB)
                    </p>
                  </div>
                  
                  {file && (
                    <div className="bg-gray-50 rounded-lg p-3">
                      <p className="text-sm" style={{ fontFamily: '"Dream Avenue"' }}>
                        Selected: {file.name} ({(file.size / 1024).toFixed(2)} KB)
                      </p>
                    </div>
                  )}

                  <Button
                    onClick={handleSubmitProof}
                    disabled={!file || uploading}
                    className="w-full"
                    style={{
                      backgroundColor: "rgba(61, 8, 17, 1)",
                      color: "rgba(255, 255, 255, 1)",
                      fontFamily: '"Dream Avenue"',
                    }}
                  >
                    {uploading ? (
                      <span className="flex items-center gap-2">
                        <span className="animate-spin">⏳</span>
                        Uploading...
                      </span>
                    ) : (
                      <span className="flex items-center gap-2">
                        <Upload className="h-4 w-4" />
                        Submit Payment Proof
                      </span>
                    )}
                  </Button>
                </div>
              </div>
            </>
          )}

          {/* Support Section */}
          <div className="bg-blue-50 border border-blue-200 rounded-lg p-6">
            <h3 className="font-semibold mb-2" style={{ fontFamily: '"Dream Avenue"' }}>
              Need Help?
            </h3>
            <p className="text-sm text-muted-foreground mb-4" style={{ fontFamily: '"Dream Avenue"' }}>
              If you have any questions or issues with your payment, contact us on WhatsApp.
            </p>
            <Button
              onClick={handleWhatsAppContact}
              variant="outline"
              className="w-full"
              style={{ fontFamily: '"Dream Avenue"' }}
            >
              <MessageCircle className="h-4 w-4 mr-2" />
              Contact us on WhatsApp
            </Button>
          </div>
        </div>
      </div>
    </div>
  )
}

