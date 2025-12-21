import { Header } from "@/components/header"
import Image from "next/image"
import Link from "next/link"
import { Button } from "@/components/ui/button"
import { ArrowLeft } from "lucide-react"
import { SpiralAnimation } from "@/components/ui/spiral-animation"

export default function AboutPage() {
  return (
    <div className="min-h-screen">
      <Header />

      <div className="container mx-auto px-4 sm:px-6 py-4">
        <Button variant="ghost" asChild style={{ fontFamily: '"Dream Avenue"' }}>
          <Link href="/">
            <ArrowLeft className="h-4 w-4 mr-2" />
            Back to Home
          </Link>
        </Button>
      </div>

      {/* Hero Section */}
      <section className="relative h-[50vh] sm:h-[60vh] flex items-center justify-center overflow-hidden" style={{ backgroundColor: '#3D0811' }}>
        <div className="absolute inset-0">
          <SpiralAnimation />
        </div>
        <div className="relative z-10 text-center px-4">
          <h1 className="font-serif text-4xl sm:text-5xl md:text-6xl lg:text-7xl font-semibold mb-4 tracking-tight text-white" style={{ fontFamily: '"Dream Avenue"' }}>
            About JST
          </h1>
          <p className="text-lg sm:text-xl text-white/90 max-w-2xl mx-auto" style={{ fontFamily: '"Dream Avenue"' }}>
            Crafting timeless elegance for the modern gentleman
          </p>
        </div>
      </section>

      {/* Story Section */}
      <section className="container mx-auto px-4 sm:px-6 py-12 sm:py-16 md:py-24">
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-8 lg:gap-16 items-center">
          <div>
            <h2 className="font-serif text-3xl sm:text-4xl md:text-5xl font-semibold mb-6" style={{ fontFamily: '"Dream Avenue"' }}>Our Story</h2>
            <div className="space-y-4 text-muted-foreground leading-relaxed">
              <p style={{ fontFamily: '"Dream Avenue"' }}>
                Founded with a vision to redefine modern menswear, Thudarum represents the perfect marriage of
                traditional craftsmanship and contemporary design. Our name, derived from the Tamil word meaning
                "continuation," embodies our commitment to carrying forward the legacy of fine tailoring into the modern
                era.
              </p>
              <p style={{ fontFamily: '"Dream Avenue"' }}>
                Every piece in our collection is meticulously crafted using premium Italian fabrics and constructed by
                master tailors who have honed their craft over decades. We believe that true luxury lies not in excess,
                but in the perfect balance of form, function, and timeless style.
              </p>
              <p style={{ fontFamily: '"Dream Avenue"' }}>
                Our double-breasted suits and blazers are designed for the discerning gentleman who appreciates quality,
                understands elegance, and values garments that will remain relevant for years to come.
              </p>
            </div>
          </div>
          <div className="relative aspect-[4/5] bg-secondary overflow-hidden">
            <Image src="/thudarum-taupe-suit-detail.jpg" alt="Thudarum craftsmanship" fill className="object-cover" />
          </div>
        </div>
      </section>

      {/* Values Section */}
    

      {/* CTA Section */}
     
    </div>
  )
}
