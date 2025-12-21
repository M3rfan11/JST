"use client";

import { useState, useEffect } from "react";
import { useRouter, useParams } from "next/navigation";
import { ArrowLeft, Save, Upload, X, Plus, Edit, Trash2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { api } from "@/lib/api-client";
import { useToast } from "@/hooks/use-toast";
import Link from "next/link";

interface Category {
  id: number;
  name: string;
}

interface VariantAttribute {
  name: string;
  values: string[];
}

interface ProductVariant {
  id?: number;
  color?: string;
  colorHex?: string;
  attributes?: string | Record<string, string>;
  priceOverride?: number;
  sku?: string;
  imageUrl?: string;
  isActive?: boolean;
}

export default function EditProductPage() {
  const router = useRouter();
  const params = useParams();
  const productId = params.id as string;
  const { toast } = useToast();
  const [loading, setLoading] = useState(false);
  const [loadingProduct, setLoadingProduct] = useState(true);
  const [categories, setCategories] = useState<Category[]>([]);
  const [selectedCategories, setSelectedCategories] = useState<number[]>([]);
  const [images, setImages] = useState<string[]>([]);
  const [imageUrlInput, setImageUrlInput] = useState("");
  const [variants, setVariants] = useState<ProductVariant[]>([]);
  const [editingVariantIndex, setEditingVariantIndex] = useState<number | null>(
    null
  );
  const [newVariant, setNewVariant] = useState<ProductVariant>({});
  const [variantAttributes, setVariantAttributes] = useState<
    VariantAttribute[]
  >([]);
  const [attributeInput, setAttributeInput] = useState("");
  const [generatedVariants, setGeneratedVariants] = useState<ProductVariant[]>(
    []
  );

  const [formData, setFormData] = useState({
    name: "",
    description: "",
    price: "0.00",
    sku: "",
    brand: "",
    washingInstructions: "",
    alwaysAvailable: false,
    isActive: true,
  });

  useEffect(() => {
    loadCategories();
    loadProduct();
  }, [productId]);

  useEffect(() => {
    generateVariants();
  }, [variantAttributes]);

  const loadCategories = async () => {
    try {
      const data = (await api.categories.getAll()) as Category[];
      setCategories(Array.isArray(data) ? data : []);
    } catch (error) {
      console.error("Error loading categories:", error);
    }
  };

  const loadProduct = async () => {
    try {
      setLoadingProduct(true);
      const product: any = await api.products.getById(productId);

      console.log("Loaded product data:", {
        id: product.id,
        name: product.name,
        isActive: product.isActive,
        status: product.status,
        alwaysAvailable: product.alwaysAvailable,
        categoryIds: product.categoryIds,
        categoryId: product.categoryId,
        mediaUrls: product.mediaUrls
          ? typeof product.mediaUrls === "string"
            ? product.mediaUrls.substring(0, 100)
            : "array"
          : null,
      });

      // Determine isActive from product status or isActive field
      // Status: 0 = Draft, 1 = Active
      // isActive: boolean
      let isActive = true; // Default to active
      if (product.isActive !== undefined) {
        isActive = product.isActive;
      } else if (product.status !== undefined) {
        isActive =
          product.status === 1 ||
          product.status === "Active" ||
          product.status === "1";
      }

      setFormData({
        name: product.name || "",
        description: product.description || "",
        price: product.price?.toString() || "0.00",
        sku: product.sku || product.sKU || "",
        brand: product.brand || "",
        washingInstructions: product.washingInstructions || "",
        alwaysAvailable: product.alwaysAvailable || false,
        isActive: isActive,
      });

      // Load categories
      if (product.categoryIds && Array.isArray(product.categoryIds)) {
        setSelectedCategories(product.categoryIds);
      } else if (product.categoryId) {
        setSelectedCategories([product.categoryId]);
      }

      // Load images
      if (product.mediaUrls) {
        try {
          const mediaUrls =
            typeof product.mediaUrls === "string"
              ? JSON.parse(product.mediaUrls)
              : product.mediaUrls;
          if (Array.isArray(mediaUrls)) {
            setImages(mediaUrls);
          } else if (product.imageUrl) {
            setImages([product.imageUrl]);
          }
        } catch {
          if (product.imageUrl) {
            setImages([product.imageUrl]);
          }
        }
      } else if (product.imageUrl) {
        setImages([product.imageUrl]);
      }

      // Load variant attributes
      if (product.variantAttributes) {
        try {
          const attrs =
            typeof product.variantAttributes === "string"
              ? JSON.parse(product.variantAttributes)
              : product.variantAttributes;
          if (Array.isArray(attrs)) {
            setVariantAttributes(attrs);
          }
        } catch {
          // If parsing fails, try to extract from existing variants
        }
      }

      // Load variants
      if (product.variants && Array.isArray(product.variants)) {
        const loadedVariants = product.variants.map((v: any) => {
          let attributes: Record<string, string> = {};
          if (v.attributes) {
            try {
              attributes =
                typeof v.attributes === "string"
                  ? JSON.parse(v.attributes)
                  : v.attributes;
            } catch {
              // If parsing fails, use color as fallback
              if (v.color) {
                attributes = { Color: v.color };
              }
            }
          } else if (v.color) {
            attributes = { Color: v.color };
          }

          return {
            id: v.id,
            color: v.color,
            colorHex: v.colorHex,
            attributes: attributes,
            priceOverride: v.priceOverride,
            sku: v.sku || v.sKU,
            imageUrl: v.imageUrl,
            isActive: v.isActive !== undefined ? v.isActive : true,
          };
        });
        setVariants(loadedVariants);

        // Extract variant attributes from existing variants if not loaded
        if (variantAttributes.length === 0 && loadedVariants.length > 0) {
          const attrMap = new Map<string, Set<string>>();
          loadedVariants.forEach((v) => {
            if (v.attributes && typeof v.attributes === "object") {
              Object.entries(v.attributes).forEach(([key, value]) => {
                if (!attrMap.has(key)) {
                  attrMap.set(key, new Set());
                }
                attrMap.get(key)!.add(value as string);
              });
            }
          });
          if (attrMap.size > 0) {
            setVariantAttributes(
              Array.from(attrMap.entries()).map(([name, values]) => ({
                name,
                values: Array.from(values),
              }))
            );
          }
        }
      } else {
        // Try to load variants from API
        try {
          const variantsData = (await api.productVariants.getByProduct(
            parseInt(productId)
          )) as any[];
          if (Array.isArray(variantsData)) {
            const loadedVariants = variantsData.map((v: any) => {
              let attributes: Record<string, string> = {};
              if (v.attributes) {
                try {
                  attributes =
                    typeof v.attributes === "string"
                      ? JSON.parse(v.attributes)
                      : v.attributes;
                } catch {
                  if (v.color) {
                    attributes = { Color: v.color };
                  }
                }
              } else if (v.color) {
                attributes = { Color: v.color };
              }

              return {
                id: v.id,
                color: v.color,
                colorHex: v.colorHex,
                attributes: attributes,
                priceOverride: v.priceOverride,
                sku: v.sku || v.sKU,
                imageUrl: v.imageUrl,
                isActive: v.isActive !== undefined ? v.isActive : true,
              };
            });
            setVariants(loadedVariants);
          }
        } catch (variantError) {
          console.error("Error loading variants:", variantError);
          // Continue without variants
        }
      }
    } catch (error: any) {
      console.error("Error loading product:", error);
      toast({
        title: "Error",
        description: error.message || "Failed to load product.",
        variant: "destructive",
      });
      router.push("/admin/products");
    } finally {
      setLoadingProduct(false);
    }
  };

  const handleCategoryToggle = (categoryId: number) => {
    setSelectedCategories((prev) =>
      prev.includes(categoryId)
        ? prev.filter((id) => id !== categoryId)
        : [...prev, categoryId]
    );
  };

  const handleImageUpload = (e: React.ChangeEvent<HTMLInputElement>) => {
    const files = e.target.files;
    if (files) {
      Array.from(files).forEach((file) => {
        // Validate file type
        if (!file.type.startsWith("image/")) {
          toast({
            title: "Invalid file type",
            description: "Please upload image files only.",
            variant: "destructive",
          });
          return;
        }

        // Validate file size (max 10MB)
        if (file.size > 10 * 1024 * 1024) {
          toast({
            title: "File too large",
            description: "Please upload images smaller than 10MB.",
            variant: "destructive",
          });
          return;
        }

        // Create a preview URL (base64 data URL)
        const reader = new FileReader();
        reader.onload = (e) => {
          if (e.target?.result) {
            setImages((prev) => [...prev, e.target.result as string]);
          }
        };
        reader.readAsDataURL(file);
      });
    }
  };

  const handleAddImageUrl = () => {
    if (imageUrlInput.trim()) {
      setImages((prev) => [...prev, imageUrlInput.trim()]);
      setImageUrlInput("");
    }
  };

  const handleRemoveImage = (index: number) => {
    setImages((prev) => prev.filter((_, i) => i !== index));
  };

  const handleAddAttribute = () => {
    if (attributeInput.trim()) {
      setVariantAttributes((prev) => [
        ...prev,
        { name: attributeInput.trim(), values: [] },
      ]);
      setAttributeInput("");
    }
  };

  const handleAddAttributeValue = (attrIndex: number, value: string) => {
    if (value.trim()) {
      setVariantAttributes((prev) => {
        const updated = [...prev];
        if (!updated[attrIndex].values.includes(value.trim())) {
          updated[attrIndex].values.push(value.trim());
        }
        return updated;
      });
    }
  };

  const handleRemoveAttribute = (index: number) => {
    setVariantAttributes((prev) => prev.filter((_, i) => i !== index));
  };

  const handleRemoveAttributeValue = (
    attrIndex: number,
    valueIndex: number
  ) => {
    setVariantAttributes((prev) => {
      const updated = [...prev];
      updated[attrIndex].values.splice(valueIndex, 1);
      return updated;
    });
  };

  const generateVariants = () => {
    if (variantAttributes.length === 0) {
      setGeneratedVariants([]);
      return;
    }

    // Generate all combinations of variant attributes
    const combinations: ProductVariant[] = [];

    const generateCombinations = (
      attrs: VariantAttribute[],
      current: Record<string, string> = {},
      index: number = 0
    ) => {
      if (index === attrs.length) {
        combinations.push({ attributes: { ...current } });
        return;
      }

      const attr = attrs[index];
      if (attr.values.length === 0) {
        generateCombinations(attrs, current, index + 1);
      } else {
        attr.values.forEach((value) => {
          generateCombinations(
            attrs,
            { ...current, [attr.name]: value },
            index + 1
          );
        });
      }
    };

    generateCombinations(variantAttributes);
    setGeneratedVariants(combinations);
  };

  const handleApplyGeneratedVariants = () => {
    // Merge generated variants with existing ones, avoiding duplicates
    const existingAttributes = new Set(
      variants.map((v) => {
        if (v.attributes && typeof v.attributes === "object") {
          return JSON.stringify(v.attributes);
        }
        return "";
      })
    );

    const newVariants = generatedVariants
      .filter((v) => {
        const attrString = JSON.stringify(v.attributes || {});
        return !existingAttributes.has(attrString);
      })
      .map((v) => ({
        ...v,
        isActive: true, // Ensure all new variants are active
      }));

    setVariants([...variants, ...newVariants]);
    toast({
      title: "Success!",
      description: `Added ${newVariants.length} new variant(s).`,
    });
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!formData.name.trim()) {
      toast({
        title: "Validation Error",
        description: "Product name is required.",
        variant: "destructive",
      });
      return;
    }

    if (selectedCategories.length === 0) {
      toast({
        title: "Validation Error",
        description: "Please select at least one category.",
        variant: "destructive",
      });
      return;
    }

    if (!formData.price || parseFloat(formData.price) <= 0) {
      toast({
        title: "Validation Error",
        description: "Please enter a valid price.",
        variant: "destructive",
      });
      return;
    }

    setLoading(true);

    try {
      const updateData = {
        name: formData.name.trim(),
        description: formData.description.trim() || null,
        price: parseFloat(formData.price) || 0,
        sku: formData.sku.trim() || null,
        brand: formData.brand.trim() || null,
        washingInstructions: formData.washingInstructions.trim() || null,
        categoryId: selectedCategories[0] || null,
        categoryIds: selectedCategories.length > 0 ? selectedCategories : null,
        isActive: formData.isActive,
        alwaysAvailable: formData.alwaysAvailable,
        inventoryTracked: !formData.alwaysAvailable,
        status: formData.isActive ? 1 : 0, // 0 = Draft, 1 = Active
        imageUrl: images[0] || null,
        mediaUrls: images.length > 0 ? JSON.stringify(images) : null,
        variantAttributes:
          variantAttributes.length > 0
            ? JSON.stringify(variantAttributes)
            : null,
        variants:
          variants.length > 0
            ? variants.map((v) => ({
                id: v.id,
                color: v.color || "",
                colorHex: v.colorHex,
                attributes:
                  typeof v.attributes === "string"
                    ? v.attributes
                    : JSON.stringify(v.attributes || {}),
                priceOverride: v.priceOverride || null,
                sku: v.sku || null,
                imageUrl: v.imageUrl || null,
                isActive: v.isActive !== false,
              }))
            : null,
      };

      console.log(
        "Updating product with data:",
        JSON.stringify(updateData, null, 2)
      );

      await api.products.update(productId, updateData);

      // Handle variants separately
      try {
        console.log("🔄 Processing variants for product:", {
          productId: parseInt(productId),
          variantsCount: variants.length,
          variants: variants,
        });

        // Get existing variants from API
        const existingVariants = (await api.productVariants.getByProduct(
          parseInt(productId)
        )) as any[];
        const existingVariantIds = existingVariants.map((v) => v.id);
        console.log(
          "📋 Existing variants:",
          existingVariants.length,
          existingVariantIds
        );

        // Update or create variants
        let createdCount = 0;
        let updatedCount = 0;
        let skippedCount = 0;

        for (const variant of variants) {
          // Extract color from attributes if not set
          let color = variant.color || "";
          if (
            !color &&
            variant.attributes &&
            typeof variant.attributes === "object"
          ) {
            // Try to get color from attributes
            color =
              variant.attributes["Color"] ||
              variant.attributes["color"] ||
              Object.values(variant.attributes)[0] ||
              "";
          }

          const attributesString =
            typeof variant.attributes === "string"
              ? variant.attributes
              : JSON.stringify(variant.attributes || {});

          const hasAttributes =
            variant.attributes &&
            typeof variant.attributes === "object" &&
            Object.keys(variant.attributes).length > 0;
          const shouldCreate =
            color || hasAttributes || attributesString !== "{}";

          console.log("🔍 Processing variant:", {
            id: variant.id,
            color,
            attributes: variant.attributes,
            attributesString,
            hasAttributes,
            shouldCreate,
            isActive: variant.isActive,
          });

          if (variant.id && existingVariantIds.includes(variant.id)) {
            // Update existing variant
            console.log("✏️ Updating existing variant:", variant.id);
            await api.productVariants.update(variant.id, {
              color: color,
              colorHex: variant.colorHex,
              attributes: attributesString,
              priceOverride: variant.priceOverride || null,
              sku: variant.sku || null,
              imageUrl: variant.imageUrl || null,
              isActive: variant.isActive !== false,
            });
            updatedCount++;
          } else if (shouldCreate) {
            // Create new variant
            console.log("➕ Creating new variant:", {
              productId: parseInt(productId),
              color: color || "",
              attributes: attributesString,
              isActive: variant.isActive !== false ? true : false,
            });
            try {
              const createdVariant = await api.productVariants.create({
                productId: parseInt(productId),
                color: color || "",
                colorHex: variant.colorHex,
                attributes: attributesString,
                priceOverride: variant.priceOverride || null,
                sku: variant.sku || null,
                imageUrl: variant.imageUrl || null,
                isActive: variant.isActive !== false ? true : false,
              });
              console.log("✅ Variant created successfully:", createdVariant);
              createdCount++;
            } catch (createError: any) {
              console.error("❌ Error creating variant:", createError);
              toast({
                title: "Error",
                description: `Failed to create variant: ${
                  createError.message || "Unknown error"
                }`,
                variant: "destructive",
              });
            }
          } else {
            console.warn(
              "⏭️ Skipping variant creation - no color or attributes:",
              variant
            );
            skippedCount++;
          }
        }

        console.log("📊 Variant processing summary:", {
          created: createdCount,
          updated: updatedCount,
          skipped: skippedCount,
          total: variants.length,
        });

        // Delete variants that were removed
        const currentVariantIds = variants
          .filter((v) => v.id)
          .map((v) => v.id!);
        const variantsToDelete = existingVariantIds.filter(
          (id) => !currentVariantIds.includes(id)
        );
        for (const variantId of variantsToDelete) {
          await api.productVariants.delete(variantId);
        }
      } catch (variantError: any) {
        console.error("Error managing variants:", variantError);
        // Don't fail the whole update if variant management fails
        toast({
          title: "Warning",
          description:
            "Product updated, but there was an issue managing variants. Please check them manually.",
          variant: "default",
        });
      }

      toast({
        title: "Success!",
        description: "Product updated successfully.",
      });

      router.push("/admin/products");
    } catch (error: any) {
      console.error("Error updating product:", error);
      console.error("Error details:", error.data || error);

      let errorMessage = "Failed to update product. Please try again.";
      if (error.message) {
        errorMessage = error.message;
      } else if (error.data) {
        if (typeof error.data === "string") {
          errorMessage = error.data;
        } else if (error.data.message) {
          errorMessage = error.data.message;
        } else if (error.data.errors) {
          const validationErrors = Object.entries(error.data.errors)
            .map(
              ([field, messages]: [string, any]) =>
                `${field}: ${
                  Array.isArray(messages) ? messages.join(", ") : messages
                }`
            )
            .join("\n");
          errorMessage = validationErrors || errorMessage;
        }
      }

      toast({
        title: "Error",
        description: errorMessage,
        variant: "destructive",
      });
    } finally {
      setLoading(false);
    }
  };

  if (loadingProduct) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="text-lg" style={{ fontFamily: '"Dream Avenue"' }}>
          Loading product...
        </div>
      </div>
    );
  }

  return (
    <div
      className="min-h-screen"
      style={{ backgroundColor: "rgba(206, 180, 157, 1)" }}
    >
      <div className="max-w-6xl mx-auto p-6">
        <div className="bg-white rounded-lg shadow-lg">
          {/* Header */}
          <div className="flex items-center justify-between p-6 border-b">
            <h1
              className="text-2xl font-semibold"
              style={{ fontFamily: '"Dream Avenue"', color: "#3D0811" }}
            >
              Edit Product
            </h1>
            <Button variant="ghost" size="icon" asChild>
              <Link href="/admin/products">
                <ArrowLeft className="h-5 w-5" />
              </Link>
            </Button>
          </div>

          <form onSubmit={handleSubmit} className="p-6 space-y-6">
            {/* Two Column Layout */}
            <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
              {/* Left Column */}
              <div className="space-y-4">
                <div className="space-y-2">
                  <Label
                    htmlFor="name"
                    style={{ fontFamily: '"Dream Avenue"' }}
                  >
                    Name
                  </Label>
                  <Input
                    id="name"
                    value={formData.name}
                    onChange={(e) =>
                      setFormData({ ...formData, name: e.target.value })
                    }
                    placeholder="Product name"
                    required
                    style={{ fontFamily: '"Dream Avenue"' }}
                  />
                </div>

                <div className="space-y-2">
                  <Label
                    htmlFor="description"
                    style={{ fontFamily: '"Dream Avenue"' }}
                  >
                    Description
                  </Label>
                  <textarea
                    id="description"
                    value={formData.description}
                    onChange={(e) =>
                      setFormData({ ...formData, description: e.target.value })
                    }
                    placeholder="Product description"
                    rows={6}
                    className="flex w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background resize-y"
                    style={{ fontFamily: '"Dream Avenue"' }}
                  />
                </div>

                <div className="space-y-2">
                  <Label
                    htmlFor="price"
                    style={{ fontFamily: '"Dream Avenue"' }}
                  >
                    Price
                  </Label>
                  <Input
                    id="price"
                    type="number"
                    step="0.01"
                    value={formData.price}
                    onChange={(e) =>
                      setFormData({ ...formData, price: e.target.value })
                    }
                    placeholder="0.00"
                    required
                    style={{ fontFamily: '"Dream Avenue"' }}
                  />
                </div>

                <div className="space-y-2">
                  <Label htmlFor="sku" style={{ fontFamily: '"Dream Avenue"' }}>
                    SKU
                  </Label>
                  <Input
                    id="sku"
                    value={formData.sku}
                    onChange={(e) =>
                      setFormData({ ...formData, sku: e.target.value })
                    }
                    placeholder="SKU"
                    style={{ fontFamily: '"Dream Avenue"' }}
                  />
                </div>
              </div>

              {/* Right Column */}
              <div className="space-y-4">
                <div className="space-y-2">
                  <Label style={{ fontFamily: '"Dream Avenue"' }}>
                    Categories (Select multiple)
                  </Label>
                  <div className="space-y-2 border rounded-md p-4 max-h-64 overflow-y-auto">
                    {categories.map((category) => (
                      <div
                        key={category.id}
                        className="flex items-center space-x-2"
                      >
                        <input
                          type="checkbox"
                          id={`category-${category.id}`}
                          checked={selectedCategories.includes(category.id)}
                          onChange={() => handleCategoryToggle(category.id)}
                          className="rounded border-gray-300"
                        />
                        <Label
                          htmlFor={`category-${category.id}`}
                          className="cursor-pointer"
                          style={{ fontFamily: '"Dream Avenue"' }}
                        >
                          {category.name}
                        </Label>
                      </div>
                    ))}
                  </div>
                </div>

                <div className="space-y-2">
                  <Label
                    htmlFor="brand"
                    style={{ fontFamily: '"Dream Avenue"' }}
                  >
                    Brand
                  </Label>
                  <Input
                    id="brand"
                    value={formData.brand}
                    onChange={(e) =>
                      setFormData({ ...formData, brand: e.target.value })
                    }
                    placeholder="Brand"
                    style={{ fontFamily: '"Dream Avenue"' }}
                  />
                </div>

                <div className="space-y-2">
                  <Label
                    htmlFor="washingInstructions"
                    style={{ fontFamily: '"Dream Avenue"' }}
                  >
                    Washing Instructions (Optional)
                  </Label>
                  <textarea
                    id="washingInstructions"
                    value={formData.washingInstructions}
                    onChange={(e) =>
                      setFormData({
                        ...formData,
                        washingInstructions: e.target.value,
                      })
                    }
                    placeholder="e.g., Machine wash cold, gentle cycle. Do not bleach. Tumble dry low."
                    rows={4}
                    className="flex w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background resize-y"
                    style={{ fontFamily: '"Dream Avenue"' }}
                  />
                </div>
              </div>
            </div>

            {/* Product Images */}
            <div className="space-y-4">
              <div>
                <h3
                  className="text-lg font-semibold mb-1"
                  style={{ fontFamily: '"Dream Avenue"', color: "#3D0811" }}
                >
                  Product Images
                </h3>
                <p
                  className="text-sm text-muted-foreground mb-4"
                  style={{ fontFamily: '"Dream Avenue"' }}
                >
                  Upload images from your computer or add image URLs. First
                  image will be used as the primary product image.
                </p>
              </div>

              {/* Upload Area */}
              <div className="border-2 border-dashed border-gray-300 rounded-lg p-6 text-center">
                <input
                  type="file"
                  id="image-upload"
                  multiple
                  accept="image/*"
                  onChange={handleImageUpload}
                  className="hidden"
                />
                <label
                  htmlFor="image-upload"
                  className="cursor-pointer flex flex-col items-center"
                >
                  <Upload className="h-10 w-10 text-gray-400 mb-3" />
                  <p
                    className="text-sm"
                    style={{ fontFamily: '"Dream Avenue"' }}
                  >
                    Click to upload images from your computer
                  </p>
                  <p
                    className="text-xs text-muted-foreground mt-1"
                    style={{ fontFamily: '"Dream Avenue"' }}
                  >
                    PNG, JPG, GIF up to 10MB
                  </p>
                </label>
              </div>

              {/* Image URL Input */}
              <div className="flex gap-2">
                <Input
                  value={imageUrlInput}
                  onChange={(e) => setImageUrlInput(e.target.value)}
                  onKeyPress={(e) =>
                    e.key === "Enter" &&
                    (e.preventDefault(), handleAddImageUrl())
                  }
                  placeholder="Or enter image URL and press Enter or click +"
                  style={{ fontFamily: '"Dream Avenue"' }}
                />
                <Button
                  type="button"
                  onClick={handleAddImageUrl}
                  style={{
                    backgroundColor: "#3D0811",
                    color: "rgba(255, 255, 255, 1)",
                    fontFamily: '"Dream Avenue"',
                  }}
                >
                  +
                </Button>
              </div>

              {/* Image Previews */}
              {images.length > 0 ? (
                <div>
                  <p
                    className="text-sm text-muted-foreground mb-2"
                    style={{ fontFamily: '"Dream Avenue"' }}
                  >
                    {images.length} {images.length === 1 ? "image" : "images"}{" "}
                    {images.length === 1 ? "loaded" : "loaded"}
                  </p>
                  <div className="grid grid-cols-4 gap-4">
                    {images.map((img, index) => (
                      <div key={index} className="relative group">
                        <img
                          src={img}
                          alt={`Product image ${index + 1}`}
                          className="w-full h-32 object-cover rounded-md border"
                          onError={(e) => {
                            console.error(
                              `Error loading image ${index + 1}:`,
                              img.substring(0, 100)
                            );
                            e.currentTarget.style.display = "none";
                          }}
                        />
                        <button
                          type="button"
                          onClick={() => handleRemoveImage(index)}
                          className="absolute top-2 right-2 bg-red-500 text-white rounded-full p-1 opacity-0 group-hover:opacity-100 transition-opacity"
                        >
                          <X className="h-4 w-4" />
                        </button>
                      </div>
                    ))}
                  </div>
                </div>
              ) : (
                <p
                  className="text-sm text-muted-foreground"
                  style={{ fontFamily: '"Dream Avenue"' }}
                >
                  No images loaded. Upload images or enter URLs above.
                </p>
              )}
            </div>

            {/* Variant Definitions */}
            <div className="space-y-4">
              <div>
                <h3
                  className="text-lg font-semibold mb-1"
                  style={{ fontFamily: '"Dream Avenue"', color: "#3D0811" }}
                >
                  Variant Definitions
                </h3>
                <p
                  className="text-sm text-muted-foreground"
                  style={{ fontFamily: '"Dream Avenue"' }}
                >
                  Add attributes and their values. All combinations will be
                  automatically generated.
                </p>
              </div>

              <div className="flex gap-2">
                <Input
                  value={attributeInput}
                  onChange={(e) => setAttributeInput(e.target.value)}
                  onKeyPress={(e) =>
                    e.key === "Enter" &&
                    (e.preventDefault(), handleAddAttribute())
                  }
                  placeholder="Attribute name (e.g., Color, Size, Parts)"
                  style={{ fontFamily: '"Dream Avenue"' }}
                />
                <Button
                  type="button"
                  onClick={handleAddAttribute}
                  style={{
                    backgroundColor: "#3D0811",
                    color: "rgba(255, 255, 255, 1)",
                    fontFamily: '"Dream Avenue"',
                  }}
                >
                  + Add
                </Button>
              </div>

              {/* Attribute List */}
              {variantAttributes.map((attr, attrIndex) => (
                <div
                  key={attrIndex}
                  className="border rounded-md p-4 space-y-2"
                >
                  <div className="flex items-center justify-between">
                    <Label
                      style={{
                        fontFamily: '"Dream Avenue"',
                        fontWeight: "bold",
                      }}
                    >
                      {attr.name}
                    </Label>
                    <Button
                      type="button"
                      variant="ghost"
                      size="sm"
                      onClick={() => handleRemoveAttribute(attrIndex)}
                    >
                      <X className="h-4 w-4" />
                    </Button>
                  </div>
                  <div className="flex flex-wrap gap-2">
                    {attr.values.map((value, valueIndex) => (
                      <span
                        key={valueIndex}
                        className="px-3 py-1 bg-gray-100 rounded-md text-sm flex items-center gap-2"
                        style={{ fontFamily: '"Dream Avenue"' }}
                      >
                        {value}
                        <button
                          type="button"
                          onClick={() =>
                            handleRemoveAttributeValue(attrIndex, valueIndex)
                          }
                          className="text-red-500 hover:text-red-700"
                        >
                          <X className="h-3 w-3" />
                        </button>
                      </span>
                    ))}
                    <Input
                      placeholder="Add value"
                      className="w-32"
                      onKeyPress={(e) => {
                        if (e.key === "Enter") {
                          e.preventDefault();
                          handleAddAttributeValue(
                            attrIndex,
                            e.currentTarget.value
                          );
                          e.currentTarget.value = "";
                        }
                      }}
                      style={{ fontFamily: '"Dream Avenue"' }}
                    />
                  </div>
                </div>
              ))}
            </div>

            {/* Product Variants */}
            <div className="space-y-4">
              <div>
                <h3
                  className="text-lg font-semibold mb-1"
                  style={{ fontFamily: '"Dream Avenue"', color: "#3D0811" }}
                >
                  Product Variants
                </h3>
                <p
                  className="text-sm text-muted-foreground"
                  style={{ fontFamily: '"Dream Avenue"' }}
                >
                  {generatedVariants.length > 0
                    ? `Configure variant attributes above to automatically generate variants. ${generatedVariants.length} variant(s) will be created.`
                    : "Configure variant attributes above to automatically generate variants."}
                </p>
              </div>

              {generatedVariants.length > 0 ? (
                <div className="border rounded-md p-4 bg-gray-50">
                  <div className="flex items-center justify-between mb-3">
                    <p
                      className="text-sm font-medium"
                      style={{ fontFamily: '"Dream Avenue"' }}
                    >
                      {generatedVariants.length} variant(s) will be created:
                    </p>
                    <Button
                      type="button"
                      onClick={handleApplyGeneratedVariants}
                      style={{
                        backgroundColor: "#3D0811",
                        color: "white",
                        fontFamily: '"Dream Avenue"',
                      }}
                    >
                      <Plus className="h-4 w-4 mr-2" />
                      Add All Variants
                    </Button>
                  </div>
                  <div className="grid grid-cols-2 md:grid-cols-3 gap-2">
                    {generatedVariants.slice(0, 12).map((variant, index) => (
                      <div
                        key={index}
                        className="text-xs p-2 bg-white rounded border"
                        style={{ fontFamily: '"Dream Avenue"' }}
                      >
                        {variant.attributes &&
                          typeof variant.attributes === "object" &&
                          Object.entries(variant.attributes).map(
                            ([key, value]) => (
                              <div key={key}>
                                {key}: {value}
                              </div>
                            )
                          )}
                      </div>
                    ))}
                  </div>
                  {generatedVariants.length > 12 && (
                    <p
                      className="text-xs text-muted-foreground mt-2"
                      style={{ fontFamily: '"Dream Avenue"' }}
                    >
                      ... and {generatedVariants.length - 12} more
                    </p>
                  )}
                </div>
              ) : variantAttributes.length > 0 &&
                variantAttributes.some((attr) => attr.values.length === 0) ? (
                <div className="border rounded-md p-4 bg-yellow-50">
                  <p
                    className="text-sm text-yellow-800"
                    style={{ fontFamily: '"Dream Avenue"' }}
                  >
                    Add values to variant attributes to generate variants.
                  </p>
                </div>
              ) : null}

              {/* Existing Variants List */}
              {variants.length > 0 && (
                <div className="border rounded-md p-4 space-y-3">
                  <h4
                    className="font-medium"
                    style={{ fontFamily: '"Dream Avenue"', color: "#3D0811" }}
                  >
                    Existing Variants ({variants.length})
                  </h4>
                  <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
                    {variants.map((variant, index) => (
                      <div
                        key={variant.id || index}
                        className="border rounded-md p-3 bg-gray-50"
                      >
                        {editingVariantIndex === index ? (
                          <div className="space-y-2">
                            <Input
                              type="number"
                              step="0.01"
                              placeholder="Price Override (optional)"
                              value={variant.priceOverride?.toString() || ""}
                              onChange={(e) => {
                                const updated = [...variants];
                                updated[index] = {
                                  ...updated[index],
                                  priceOverride: e.target.value
                                    ? parseFloat(e.target.value)
                                    : undefined,
                                };
                                setVariants(updated);
                              }}
                              style={{ fontFamily: '"Dream Avenue"' }}
                            />
                            <Input
                              placeholder="SKU (optional)"
                              value={variant.sku || ""}
                              onChange={(e) => {
                                const updated = [...variants];
                                updated[index] = {
                                  ...updated[index],
                                  sku: e.target.value,
                                };
                                setVariants(updated);
                              }}
                              style={{ fontFamily: '"Dream Avenue"' }}
                            />
                            <div className="flex gap-2">
                              <Button
                                type="button"
                                size="sm"
                                onClick={() => setEditingVariantIndex(null)}
                                style={{
                                  backgroundColor: "#3D0811",
                                  color: "white",
                                  fontFamily: '"Dream Avenue"',
                                }}
                              >
                                Save
                              </Button>
                              <Button
                                type="button"
                                size="sm"
                                variant="outline"
                                onClick={() => {
                                  setEditingVariantIndex(null);
                                  loadProduct();
                                }}
                                style={{ fontFamily: '"Dream Avenue"' }}
                              >
                                Cancel
                              </Button>
                            </div>
                          </div>
                        ) : (
                          <div className="space-y-2">
                            <div className="flex items-center justify-between">
                              <div>
                                {variant.attributes &&
                                typeof variant.attributes === "object" ? (
                                  <div className="text-sm">
                                    {Object.entries(variant.attributes).map(
                                      ([key, value]) => (
                                        <div
                                          key={key}
                                          style={{
                                            fontFamily: '"Dream Avenue"',
                                          }}
                                        >
                                          <span className="font-medium">
                                            {key}:
                                          </span>{" "}
                                          {value}
                                        </div>
                                      )
                                    )}
                                  </div>
                                ) : (
                                  <span
                                    className="font-medium"
                                    style={{ fontFamily: '"Dream Avenue"' }}
                                  >
                                    {variant.color || "Unnamed Variant"}
                                  </span>
                                )}
                              </div>
                              <div className="flex gap-1">
                                <Button
                                  type="button"
                                  size="sm"
                                  variant="ghost"
                                  onClick={() => setEditingVariantIndex(index)}
                                >
                                  <Edit className="h-3 w-3" />
                                </Button>
                                <Button
                                  type="button"
                                  size="sm"
                                  variant="ghost"
                                  onClick={() => {
                                    const updated = variants.filter(
                                      (_, i) => i !== index
                                    );
                                    setVariants(updated);
                                  }}
                                >
                                  <Trash2 className="h-3 w-3 text-red-500" />
                                </Button>
                              </div>
                            </div>
                            {variant.priceOverride && (
                              <p
                                className="text-sm text-muted-foreground"
                                style={{ fontFamily: '"Dream Avenue"' }}
                              >
                                Price: EGP {variant.priceOverride.toFixed(2)}
                              </p>
                            )}
                            {variant.sku && (
                              <p
                                className="text-sm text-muted-foreground"
                                style={{ fontFamily: '"Dream Avenue"' }}
                              >
                                SKU: {variant.sku}
                              </p>
                            )}
                            {variant.isActive === false && (
                              <span
                                className="text-xs text-red-500"
                                style={{ fontFamily: '"Dream Avenue"' }}
                              >
                                Inactive
                              </span>
                            )}
                          </div>
                        )}
                      </div>
                    ))}
                  </div>
                </div>
              )}
            </div>

            {/* Always Available Toggle */}
            <div className="flex items-center justify-between p-4 border rounded-md">
              <div>
                <Label
                  style={{ fontFamily: '"Dream Avenue"', fontWeight: "bold" }}
                >
                  Always Available
                </Label>
                <p
                  className="text-sm text-muted-foreground"
                  style={{ fontFamily: '"Dream Avenue"' }}
                >
                  Product is always available regardless of stock level
                </p>
              </div>
              <label className="relative inline-flex items-center cursor-pointer">
                <input
                  type="checkbox"
                  checked={formData.alwaysAvailable}
                  onChange={(e) =>
                    setFormData({
                      ...formData,
                      alwaysAvailable: e.target.checked,
                    })
                  }
                  className="sr-only peer"
                />
                <div className="w-11 h-6 bg-gray-200 peer-focus:outline-none rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-gray-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-green-500"></div>
              </label>
            </div>

            {/* Product Status Toggle */}
            <div className="flex items-center justify-between p-4 border rounded-md">
              <div>
                <Label
                  style={{ fontFamily: '"Dream Avenue"', fontWeight: "bold" }}
                >
                  Product Status
                </Label>
                <p
                  className="text-sm text-muted-foreground"
                  style={{ fontFamily: '"Dream Avenue"' }}
                >
                  Active products appear to customers, Draft products are hidden
                </p>
              </div>
              <label className="relative inline-flex items-center cursor-pointer">
                <input
                  type="checkbox"
                  checked={formData.isActive}
                  onChange={(e) =>
                    setFormData({ ...formData, isActive: e.target.checked })
                  }
                  className="sr-only peer"
                />
                <div className="w-11 h-6 bg-gray-200 peer-focus:outline-none rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-gray-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-green-500"></div>
                {formData.isActive && (
                  <span
                    className="ml-3 text-sm text-green-600"
                    style={{ fontFamily: '"Dream Avenue"' }}
                  >
                    ✓ Active
                  </span>
                )}
              </label>
            </div>

            {/* Action Buttons */}
            <div className="flex items-center justify-end gap-4 pt-4 border-t">
              <Button
                type="button"
                variant="outline"
                onClick={() => router.back()}
                className="bg-white"
                style={{ fontFamily: '"Dream Avenue"' }}
              >
                Cancel
              </Button>
              <Button
                type="submit"
                disabled={loading}
                style={{
                  backgroundColor: "#3D0811",
                  color: "rgba(255, 255, 255, 1)",
                  fontFamily: '"Dream Avenue"',
                }}
              >
                <Save className="h-4 w-4 mr-2" />
                {loading ? "Updating..." : "Update Product"}
              </Button>
            </div>
          </form>
        </div>
      </div>
    </div>
  );
}
