"use client"

import { useState, useEffect } from "react"
import { Header } from "@/components/header"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { useToast } from "@/hooks/use-toast"
import { CheckCircle, XCircle, Eye, Download, RefreshCw, FileImage, FileText } from "lucide-react"
import { api } from "@/lib/api-client"
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription } from "@/components/ui/dialog"
import Image from "next/image"

interface PaymentProof {
  id: number
  fileUrl: string
  fileType: string
  fileName?: string
  uploadedAt: string
}

interface PendingOrder {
  id: number
  orderNumber: string
  customerName: string
  customerPhone?: string
  customerEmail?: string
  totalAmount: number
  status: string
  createdAt: string
  latestProof?: PaymentProof
}

export default function InstaPayReviewPage() {
  const { toast } = useToast()
  const [orders, setOrders] = useState<PendingOrder[]>([])
  const [loading, setLoading] = useState(true)
  const [selectedOrder, setSelectedOrder] = useState<PendingOrder | null>(null)
  const [showAcceptDialog, setShowAcceptDialog] = useState(false)
  const [showRejectDialog, setShowRejectDialog] = useState(false)
  const [adminNote, setAdminNote] = useState("")
  const [rejectionReason, setRejectionReason] = useState("")
  const [processing, setProcessing] = useState(false)

  useEffect(() => {
    loadPendingProofs()
  }, [])

  const loadPendingProofs = async () => {
    try {
      setLoading(true)
      const response = await api.instapay.getPendingProofs() as any
      setOrders(response || [])
    } catch (error: any) {
      console.error("Error loading pending proofs:", error)
      toast({
        title: "Error",
        description: error.message || "Failed to load pending proofs.",
        variant: "destructive",
      })
    } finally {
      setLoading(false)
    }
  }

  const handleAccept = async () => {
    if (!selectedOrder) return

    try {
      setProcessing(true)
      await api.instapay.acceptPayment(selectedOrder.id, { adminNote })
      
      toast({
        title: "Payment Accepted",
        description: `Order #${selectedOrder.orderNumber} payment has been accepted.`,
      })
      
      setShowAcceptDialog(false)
      setAdminNote("")
      setSelectedOrder(null)
      await loadPendingProofs()
    } catch (error: any) {
      console.error("Error accepting payment:", error)
      toast({
        title: "Error",
        description: error.message || "Failed to accept payment.",
        variant: "destructive",
      })
    } finally {
      setProcessing(false)
    }
  }

  const handleReject = async () => {
    if (!selectedOrder || !rejectionReason.trim()) {
      toast({
        title: "Error",
        description: "Please provide a rejection reason.",
        variant: "destructive",
      })
      return
    }

    try {
      setProcessing(true)
      await api.instapay.rejectPayment(selectedOrder.id, {
        rejectionReason,
        adminNote,
      })
      
      toast({
        title: "Payment Rejected",
        description: `Order #${selectedOrder.orderNumber} payment has been rejected.`,
      })
      
      setShowRejectDialog(false)
      setRejectionReason("")
      setAdminNote("")
      setSelectedOrder(null)
      await loadPendingProofs()
    } catch (error: any) {
      console.error("Error rejecting payment:", error)
      toast({
        title: "Error",
        description: error.message || "Failed to reject payment.",
        variant: "destructive",
      })
    } finally {
      setProcessing(false)
    }
  }

  const [showProofDialog, setShowProofDialog] = useState(false)
  const [viewingProof, setViewingProof] = useState<PaymentProof | null>(null)

  const openProof = (proof: PaymentProof) => {
    setViewingProof(proof)
    setShowProofDialog(true)
  }

  const getProofUrl = (proof: PaymentProof) => {
    return `${process.env.NEXT_PUBLIC_API_URL || 'http://localhost:8081'}${proof.fileUrl}`
  }

  const isImageFile = (fileType: string) => {
    return fileType.startsWith("image/")
  }

  const formatCurrency = (value: number) => `${value.toLocaleString('en-EG', { minimumFractionDigits: 2 })} EGP`

  return (
    <div className="min-h-screen bg-background">
      <Header />
      <div className="container mx-auto px-4 sm:px-6 py-6 sm:py-8 md:py-12">
        <div className="flex items-center justify-between mb-6">
          <div>
            <h1 className="font-serif text-3xl sm:text-4xl font-semibold mb-2" style={{ fontFamily: '"Dream Avenue"', color: '#3D0811' }}>
              InstaPay Payment Review
            </h1>
            <p className="text-muted-foreground" style={{ fontFamily: '"Dream Avenue"' }}>
              Review and verify payment proofs for InstaPay orders
            </p>
          </div>
          <Button
            onClick={loadPendingProofs}
            variant="outline"
            className="gap-2"
            style={{ fontFamily: '"Dream Avenue"' }}
          >
            <RefreshCw className="h-4 w-4" />
            Refresh
          </Button>
        </div>

        {loading ? (
          <div className="text-center py-12">
            <p style={{ fontFamily: '"Dream Avenue"' }}>Loading pending proofs...</p>
          </div>
        ) : orders.length === 0 ? (
          <div className="text-center py-12 bg-white rounded-lg border">
            <p className="text-muted-foreground" style={{ fontFamily: '"Dream Avenue"' }}>
              No pending payment proofs to review
            </p>
          </div>
        ) : (
          <div className="space-y-4">
            {orders.map((order) => (
              <div
                key={order.id}
                className="bg-white border border-border rounded-lg p-6"
              >
                <div className="flex items-start justify-between mb-4">
                  <div className="flex-1">
                    <h3 className="font-semibold text-lg mb-1" style={{ fontFamily: '"Dream Avenue"', color: '#3D0811' }}>
                      Order #{order.orderNumber}
                    </h3>
                    <div className="flex gap-6">
                      <div className="text-sm text-muted-foreground space-y-1" style={{ fontFamily: '"Dream Avenue"' }}>
                        <p><strong>Customer:</strong> {order.customerName}</p>
                        {order.customerPhone && <p><strong>Phone:</strong> {order.customerPhone}</p>}
                        {order.customerEmail && <p><strong>Email:</strong> {order.customerEmail}</p>}
                        <p><strong>Amount:</strong> {formatCurrency(order.totalAmount)}</p>
                        <p><strong>Status:</strong> {order.status}</p>
                        <p><strong>Created:</strong> {new Date(order.createdAt).toLocaleString()}</p>
                      </div>
                      {/* Proof Preview Thumbnail */}
                      {order.latestProof && (
                        <div className="flex-shrink-0">
                          <Label className="text-xs text-muted-foreground mb-2 block" style={{ fontFamily: '"Dream Avenue"' }}>
                            Proof Preview:
                          </Label>
                          <div 
                            className="relative w-32 h-32 bg-gray-100 rounded-lg border border-border overflow-hidden cursor-pointer hover:opacity-80 transition-opacity"
                            onClick={() => openProof(order.latestProof!)}
                            title="Click to view full size"
                          >
                            {isImageFile(order.latestProof.fileType) ? (
                              <Image
                                src={getProofUrl(order.latestProof)}
                                alt="Payment Proof"
                                fill
                                className="object-cover"
                                unoptimized
                              />
                            ) : (
                              <div className="w-full h-full flex items-center justify-center">
                                <FileText className="h-8 w-8 text-gray-400" />
                              </div>
                            )}
                          </div>
                          <p className="text-xs text-muted-foreground mt-1" style={{ fontFamily: '"Dream Avenue"' }}>
                            {new Date(order.latestProof.uploadedAt).toLocaleDateString()}
                          </p>
                        </div>
                      )}
                    </div>
                  </div>
                  <div className="flex gap-2">
                    {order.latestProof && (
                      <>
                        <Button
                          variant="outline"
                          size="sm"
                          onClick={() => openProof(order.latestProof!)}
                          className="gap-2"
                          style={{ fontFamily: '"Dream Avenue"' }}
                        >
                          <Eye className="h-4 w-4" />
                          View Proof
                        </Button>
                        <Button
                          variant="outline"
                          size="sm"
                          onClick={() => window.open(getProofUrl(order.latestProof!), "_blank")}
                          className="gap-2"
                          style={{ fontFamily: '"Dream Avenue"' }}
                          title="Open in new tab"
                        >
                          <Download className="h-4 w-4" />
                        </Button>
                      </>
                    )}
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => {
                        setSelectedOrder(order)
                        setShowAcceptDialog(true)
                      }}
                      className="gap-2 text-green-600 border-green-600 hover:bg-green-50"
                      style={{ fontFamily: '"Dream Avenue"' }}
                    >
                      <CheckCircle className="h-4 w-4" />
                      Accept
                    </Button>
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => {
                        setSelectedOrder(order)
                        setShowRejectDialog(true)
                      }}
                      className="gap-2 text-red-600 border-red-600 hover:bg-red-50"
                      style={{ fontFamily: '"Dream Avenue"' }}
                    >
                      <XCircle className="h-4 w-4" />
                      Reject
                    </Button>
                  </div>
                </div>
              </div>
            ))}
          </div>
        )}

        {/* Accept Dialog */}
        <Dialog open={showAcceptDialog} onOpenChange={setShowAcceptDialog}>
          <DialogContent className="max-w-3xl max-h-[90vh] overflow-y-auto">
            <DialogHeader>
              <DialogTitle style={{ fontFamily: '"Dream Avenue"' }}>
                Accept Payment - Order #{selectedOrder?.orderNumber}
              </DialogTitle>
              <DialogDescription style={{ fontFamily: '"Dream Avenue"' }}>
                Review the payment proof below and confirm that payment has been received.
              </DialogDescription>
            </DialogHeader>
            <div className="space-y-4">
              {/* Payment Proof Preview */}
              {selectedOrder?.latestProof && (
                <div className="border border-border rounded-lg p-4 bg-gray-50">
                  <div className="flex items-center justify-between mb-3">
                    <Label className="text-sm font-semibold" style={{ fontFamily: '"Dream Avenue"' }}>
                      Payment Proof
                    </Label>
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => window.open(getProofUrl(selectedOrder.latestProof!), "_blank")}
                      className="gap-2 text-xs"
                      style={{ fontFamily: '"Dream Avenue"' }}
                    >
                      <Download className="h-3 w-3" />
                      Open Full Size
                    </Button>
                  </div>
                  <div className="bg-white rounded-lg p-4 flex justify-center items-center min-h-[300px] max-h-[500px] overflow-auto">
                    {isImageFile(selectedOrder.latestProof.fileType) ? (
                      <Image
                        src={getProofUrl(selectedOrder.latestProof)}
                        alt="Payment Proof"
                        width={600}
                        height={600}
                        className="object-contain max-w-full max-h-full"
                        unoptimized
                      />
                    ) : (
                      <div className="text-center">
                        <FileText className="h-16 w-16 text-gray-400 mx-auto mb-2" />
                        <p className="text-sm text-muted-foreground mb-3" style={{ fontFamily: '"Dream Avenue"' }}>
                          PDF Document
                        </p>
                        <Button
                          variant="outline"
                          onClick={() => window.open(getProofUrl(selectedOrder.latestProof!), "_blank")}
                          style={{ fontFamily: '"Dream Avenue"' }}
                        >
                          Open PDF
                        </Button>
                      </div>
                    )}
                  </div>
                  <div className="mt-2 text-xs text-muted-foreground" style={{ fontFamily: '"Dream Avenue"' }}>
                    Uploaded: {new Date(selectedOrder.latestProof.uploadedAt).toLocaleString()}
                    {selectedOrder.latestProof.fileName && ` • ${selectedOrder.latestProof.fileName}`}
                  </div>
                </div>
              )}
              
              <div>
                <Label htmlFor="adminNote" style={{ fontFamily: '"Dream Avenue"' }}>
                  Admin Note (Optional)
                </Label>
                <Input
                  id="adminNote"
                  value={adminNote}
                  onChange={(e) => setAdminNote(e.target.value)}
                  placeholder="Add any notes about this payment..."
                  className="mt-1.5"
                />
              </div>
              <div className="flex gap-2 justify-end">
                <Button
                  variant="outline"
                  onClick={() => {
                    setShowAcceptDialog(false)
                    setAdminNote("")
                  }}
                  style={{ fontFamily: '"Dream Avenue"' }}
                >
                  Cancel
                </Button>
                <Button
                  onClick={handleAccept}
                  disabled={processing}
                  style={{
                    backgroundColor: "rgba(61, 8, 17, 1)",
                    color: "rgba(255, 255, 255, 1)",
                    fontFamily: '"Dream Avenue"',
                  }}
                >
                  {processing ? "Processing..." : "Accept Payment"}
                </Button>
              </div>
            </div>
          </DialogContent>
        </Dialog>

        {/* Reject Dialog */}
        <Dialog open={showRejectDialog} onOpenChange={setShowRejectDialog}>
          <DialogContent className="max-w-3xl max-h-[90vh] overflow-y-auto">
            <DialogHeader>
              <DialogTitle style={{ fontFamily: '"Dream Avenue"' }}>
                Reject Payment - Order #{selectedOrder?.orderNumber}
              </DialogTitle>
              <DialogDescription style={{ fontFamily: '"Dream Avenue"' }}>
                Review the payment proof below and provide a reason for rejection.
              </DialogDescription>
            </DialogHeader>
            <div className="space-y-4">
              {/* Payment Proof Preview */}
              {selectedOrder?.latestProof && (
                <div className="border border-border rounded-lg p-4 bg-gray-50">
                  <div className="flex items-center justify-between mb-3">
                    <Label className="text-sm font-semibold" style={{ fontFamily: '"Dream Avenue"' }}>
                      Payment Proof
                    </Label>
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => window.open(getProofUrl(selectedOrder.latestProof!), "_blank")}
                      className="gap-2 text-xs"
                      style={{ fontFamily: '"Dream Avenue"' }}
                    >
                      <Download className="h-3 w-3" />
                      Open Full Size
                    </Button>
                  </div>
                  <div className="bg-white rounded-lg p-4 flex justify-center items-center min-h-[300px] max-h-[500px] overflow-auto">
                    {isImageFile(selectedOrder.latestProof.fileType) ? (
                      <Image
                        src={getProofUrl(selectedOrder.latestProof)}
                        alt="Payment Proof"
                        width={600}
                        height={600}
                        className="object-contain max-w-full max-h-full"
                        unoptimized
                      />
                    ) : (
                      <div className="text-center">
                        <FileText className="h-16 w-16 text-gray-400 mx-auto mb-2" />
                        <p className="text-sm text-muted-foreground mb-3" style={{ fontFamily: '"Dream Avenue"' }}>
                          PDF Document
                        </p>
                        <Button
                          variant="outline"
                          onClick={() => window.open(getProofUrl(selectedOrder.latestProof!), "_blank")}
                          style={{ fontFamily: '"Dream Avenue"' }}
                        >
                          Open PDF
                        </Button>
                      </div>
                    )}
                  </div>
                  <div className="mt-2 text-xs text-muted-foreground" style={{ fontFamily: '"Dream Avenue"' }}>
                    Uploaded: {new Date(selectedOrder.latestProof.uploadedAt).toLocaleString()}
                    {selectedOrder.latestProof.fileName && ` • ${selectedOrder.latestProof.fileName}`}
                  </div>
                </div>
              )}
              
              <div>
                <Label htmlFor="rejectionReason" style={{ fontFamily: '"Dream Avenue"' }}>
                  Rejection Reason <span className="text-red-500">*</span>
                </Label>
                <Input
                  id="rejectionReason"
                  value={rejectionReason}
                  onChange={(e) => setRejectionReason(e.target.value)}
                  placeholder="e.g., Wrong amount, Unclear proof, Payment not received"
                  className="mt-1.5"
                  required
                />
              </div>
              <div>
                <Label htmlFor="rejectAdminNote" style={{ fontFamily: '"Dream Avenue"' }}>
                  Admin Note (Optional)
                </Label>
                <Input
                  id="rejectAdminNote"
                  value={adminNote}
                  onChange={(e) => setAdminNote(e.target.value)}
                  placeholder="Add any additional notes..."
                  className="mt-1.5"
                />
              </div>
              <div className="flex gap-2 justify-end">
                <Button
                  variant="outline"
                  onClick={() => {
                    setShowRejectDialog(false)
                    setRejectionReason("")
                    setAdminNote("")
                  }}
                  style={{ fontFamily: '"Dream Avenue"' }}
                >
                  Cancel
                </Button>
                <Button
                  onClick={handleReject}
                  disabled={processing || !rejectionReason.trim()}
                  className="bg-red-600 hover:bg-red-700"
                  style={{ fontFamily: '"Dream Avenue"' }}
                >
                  {processing ? "Processing..." : "Reject Payment"}
                </Button>
              </div>
            </div>
          </DialogContent>
        </Dialog>

        {/* View Proof Dialog */}
        <Dialog open={showProofDialog} onOpenChange={setShowProofDialog}>
          <DialogContent className="max-w-4xl max-h-[90vh] overflow-y-auto">
            <DialogHeader>
              <DialogTitle style={{ fontFamily: '"Dream Avenue"' }}>
                Payment Proof
              </DialogTitle>
              <DialogDescription style={{ fontFamily: '"Dream Avenue"' }}>
                {viewingProof?.fileName || "Payment proof document"}
              </DialogDescription>
            </DialogHeader>
            {viewingProof && (
              <div className="space-y-4">
                <div className="bg-white rounded-lg p-4 flex justify-center items-center min-h-[400px] max-h-[600px] overflow-auto border border-border">
                  {isImageFile(viewingProof.fileType) ? (
                    <Image
                      src={getProofUrl(viewingProof)}
                      alt="Payment Proof"
                      width={800}
                      height={800}
                      className="object-contain max-w-full max-h-full"
                      unoptimized
                    />
                  ) : (
                    <div className="text-center">
                      <FileText className="h-24 w-24 text-gray-400 mx-auto mb-4" />
                      <p className="text-muted-foreground mb-4" style={{ fontFamily: '"Dream Avenue"' }}>
                        PDF Document
                      </p>
                      <Button
                        variant="outline"
                        onClick={() => window.open(getProofUrl(viewingProof), "_blank")}
                        style={{ fontFamily: '"Dream Avenue"' }}
                      >
                        <Download className="h-4 w-4 mr-2" />
                        Open PDF in New Tab
                      </Button>
                    </div>
                  )}
                </div>
                <div className="text-sm text-muted-foreground space-y-1" style={{ fontFamily: '"Dream Avenue"' }}>
                  <p><strong>File Type:</strong> {viewingProof.fileType}</p>
                  {viewingProof.fileName && <p><strong>File Name:</strong> {viewingProof.fileName}</p>}
                  <p><strong>Uploaded:</strong> {new Date(viewingProof.uploadedAt).toLocaleString()}</p>
                </div>
                <div className="flex justify-end">
                  <Button
                    variant="outline"
                    onClick={() => setShowProofDialog(false)}
                    style={{ fontFamily: '"Dream Avenue"' }}
                  >
                    Close
                  </Button>
                </div>
              </div>
            )}
          </DialogContent>
        </Dialog>
      </div>
    </div>
  )
}

