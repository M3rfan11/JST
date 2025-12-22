"use client"

import { BarChart3 } from "lucide-react"

export default function ReportsPage() {
  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-semibold mb-2" style={{ fontFamily: '"Dream Avenue"', color: '#3D0811' }}>
          Reports
        </h1>
        <p className="text-muted-foreground" style={{ fontFamily: '"Dream Avenue"' }}>
          View sales reports, analytics, and insights
        </p>
      </div>

      <div className="bg-white rounded-lg p-8 border border-border text-center">
        <BarChart3 className="h-12 w-12 mx-auto mb-4 text-muted-foreground" />
        <p className="text-muted-foreground" style={{ fontFamily: '"Dream Avenue"' }}>
          Reports and analytics features coming soon
        </p>
      </div>
    </div>
  )
}







