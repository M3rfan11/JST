"use client"

import { useState, useEffect } from "react"
import { Search, Edit, Trash2 } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { api } from "@/lib/api-client"

interface User {
  id: number
  fullName: string
  email: string
  isActive: boolean
  roles: string[]
  createdAt: string
}

export default function UsersPage() {
  const [users, setUsers] = useState<User[]>([])
  const [loading, setLoading] = useState(true)
  const [searchTerm, setSearchTerm] = useState("")

  useEffect(() => {
    loadUsers()
  }, [])

  const loadUsers = async () => {
    try {
      setLoading(true)
      const data = await api.admin.getUsers()
      setUsers(data)
    } catch (error) {
      console.error("Error loading users:", error)
    } finally {
      setLoading(false)
    }
  }

  const filteredUsers = users.filter(
    (user) =>
      user.fullName.toLowerCase().includes(searchTerm.toLowerCase()) ||
      user.email.toLowerCase().includes(searchTerm.toLowerCase())
  )

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="text-lg" style={{ fontFamily: '"Dream Avenue"' }}>Loading users...</div>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-semibold mb-2" style={{ fontFamily: '"Dream Avenue"', color: '#3D0811' }}>
          Users & Roles
        </h1>
        <p className="text-muted-foreground" style={{ fontFamily: '"Dream Avenue"' }}>
          Manage users and their roles
        </p>
      </div>

      <div className="bg-white rounded-lg p-4 border border-border">
        <div className="relative">
          <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-muted-foreground" />
          <Input
            placeholder="Search users by name or email..."
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            className="pl-10"
            style={{ fontFamily: '"Dream Avenue"' }}
          />
        </div>
      </div>

      <div className="bg-white rounded-lg border border-border overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full">
            <thead style={{ backgroundColor: '#3D0811' }}>
              <tr>
                <th className="px-6 py-3 text-left text-sm font-medium text-white" style={{ fontFamily: '"Dream Avenue"' }}>
                  Name
                </th>
                <th className="px-6 py-3 text-left text-sm font-medium text-white" style={{ fontFamily: '"Dream Avenue"' }}>
                  Email
                </th>
                <th className="px-6 py-3 text-left text-sm font-medium text-white" style={{ fontFamily: '"Dream Avenue"' }}>
                  Roles
                </th>
                <th className="px-6 py-3 text-left text-sm font-medium text-white" style={{ fontFamily: '"Dream Avenue"' }}>
                  Status
                </th>
                <th className="px-6 py-3 text-left text-sm font-medium text-white" style={{ fontFamily: '"Dream Avenue"' }}>
                  Created
                </th>
                <th className="px-6 py-3 text-right text-sm font-medium text-white" style={{ fontFamily: '"Dream Avenue"' }}>
                  Actions
                </th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border">
              {filteredUsers.length === 0 ? (
                <tr>
                  <td colSpan={6} className="px-6 py-8 text-center text-muted-foreground" style={{ fontFamily: '"Dream Avenue"' }}>
                    No users found
                  </td>
                </tr>
              ) : (
                filteredUsers.map((user) => (
                  <tr key={user.id} className="hover:bg-muted/50">
                    <td className="px-6 py-4 font-medium" style={{ fontFamily: '"Dream Avenue"' }}>
                      {user.fullName}
                    </td>
                    <td className="px-6 py-4 text-muted-foreground" style={{ fontFamily: '"Dream Avenue"' }}>
                      {user.email}
                    </td>
                    <td className="px-6 py-4">
                      <div className="flex flex-wrap gap-1">
                        {user.roles.map((role) => (
                          <span
                            key={role}
                            className="px-2 py-1 rounded text-xs bg-blue-100 text-blue-800"
                            style={{ fontFamily: '"Dream Avenue"' }}
                          >
                            {role}
                          </span>
                        ))}
                      </div>
                    </td>
                    <td className="px-6 py-4">
                      <span
                        className={`px-2 py-1 rounded text-xs ${
                          user.isActive ? "bg-green-100 text-green-800" : "bg-gray-100 text-gray-800"
                        }`}
                        style={{ fontFamily: '"Dream Avenue"' }}
                      >
                        {user.isActive ? "Active" : "Inactive"}
                      </span>
                    </td>
                    <td className="px-6 py-4 text-muted-foreground" style={{ fontFamily: '"Dream Avenue"' }}>
                      {new Date(user.createdAt).toLocaleDateString()}
                    </td>
                    <td className="px-6 py-4 text-right">
                      <div className="flex items-center justify-end gap-2">
                        <Button variant="ghost" size="sm">
                          <Edit className="h-4 w-4" />
                        </Button>
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={() => {
                            // TODO: Implement delete
                            console.log("Delete user", user.id)
                          }}
                        >
                          <Trash2 className="h-4 w-4 text-red-500" />
                        </Button>
                      </div>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  )
}











