"use client"

import { useEffect, useRef } from "react"

export function HeroBackground() {
  const canvasRef = useRef<HTMLCanvasElement>(null)

  useEffect(() => {
    const canvas = canvasRef.current
    if (!canvas) return

    const gl = canvas.getContext("webgl")
    if (!gl) return

    const vertexShaderSource = `
      attribute vec2 position;
      void main() {
        gl_Position = vec4(position, 0.0, 1.0);
      }
    `

    const fragmentShaderSource = `
      precision mediump float;
      uniform float u_time;
      uniform vec2 u_resolution;

      // Simple noise function
      float noise(vec2 p) {
        return fract(sin(dot(p, vec2(12.9898, 78.233))) * 43758.5453);
      }

      // Smooth noise
      float smoothNoise(vec2 p) {
        vec2 i = floor(p);
        vec2 f = fract(p);
        f = f * f * (3.0 - 2.0 * f);
        float a = noise(i);
        float b = noise(i + vec2(1.0, 0.0));
        float c = noise(i + vec2(0.0, 1.0));
        float d = noise(i + vec2(1.0, 1.0));
        return mix(mix(a, b, f.x), mix(c, d, f.x), f.y);
      }

      // Fractional Brownian Motion
      float fbm(vec2 p) {
        float value = 0.0;
        float amplitude = 0.5;
        for (int i = 0; i < 5; i++) {
          value += amplitude * smoothNoise(p);
          p *= 2.0;
          amplitude *= 0.5;
        }
        return value;
      }

      void main() {
        vec2 uv = gl_FragCoord.xy / u_resolution.xy;
        float time = u_time * 0.1;
        
        // Create a flowing effect
        vec2 q = vec2(0.);
        q.x = fbm(uv + 0.0 * time);
        q.y = fbm(uv + vec2(1.0));

        vec2 r = vec2(0.);
        r.x = fbm(uv + 1.0 * q + vec2(1.7, 9.2) + 0.15 * time);
        r.y = fbm(uv + 1.0 * q + vec2(8.3, 2.8) + 0.126 * time);

        float f = fbm(uv + r);

        // Mix colors based on noise
        // Deep Maroon: #3d0811 -> (61/255, 8/255, 17/255) -> (0.239, 0.031, 0.067)
        // Black/Dark Grey: #0f0f0f -> (15/255, 15/255, 15/255) -> (0.059, 0.059, 0.059)
        // Beige Accent: #ceb49d -> (206/255, 180/255, 157/255) -> (0.808, 0.706, 0.616)
        
        vec3 color1 = vec3(0.059, 0.059, 0.059); // Dark base
        vec3 color2 = vec3(0.239, 0.031, 0.067); // Deep Maroon
        vec3 color3 = vec3(0.1, 0.0, 0.0); // Very dark red/black

        // Mix based on noise flow
        vec3 color = mix(color1, color2, clamp((f*f)*3.5, 0.0, 1.0));
        color = mix(color, color3, clamp(length(q), 0.0, 1.0));
        
        // Add subtle vignette
        float vignette = 1.0 - length(uv - 0.5) * 0.5;
        color *= vignette;

        gl_FragColor = vec4(color, 1.0);
      }
    `

    // Compile shaders
    const createShader = (type: number, source: string) => {
      const shader = gl.createShader(type)
      if (!shader) return null
      gl.shaderSource(shader, source)
      gl.compileShader(shader)
      if (!gl.getShaderParameter(shader, gl.COMPILE_STATUS)) {
        console.error(gl.getShaderInfoLog(shader))
        gl.deleteShader(shader)
        return null
      }
      return shader
    }

    const vertexShader = createShader(gl.VERTEX_SHADER, vertexShaderSource)
    const fragmentShader = createShader(gl.FRAGMENT_SHADER, fragmentShaderSource)
    if (!vertexShader || !fragmentShader) return

    const program = gl.createProgram()
    if (!program) return
    gl.attachShader(program, vertexShader)
    gl.attachShader(program, fragmentShader)
    gl.linkProgram(program)
    if (!gl.getProgramParameter(program, gl.LINK_STATUS)) {
      console.error(gl.getProgramInfoLog(program))
      return
    }

    gl.useProgram(program)

    // Set up geometry (full screen quad)
    const positionBuffer = gl.createBuffer()
    gl.bindBuffer(gl.ARRAY_BUFFER, positionBuffer)
    gl.bufferData(gl.ARRAY_BUFFER, new Float32Array([
      -1, -1,
      1, -1,
      -1, 1,
      -1, 1,
      1, -1,
      1, 1,
    ]), gl.STATIC_DRAW)

    const positionAttributeLocation = gl.getAttribLocation(program, "position")
    gl.enableVertexAttribArray(positionAttributeLocation)
    gl.vertexAttribPointer(positionAttributeLocation, 2, gl.FLOAT, false, 0, 0)

    const timeLocation = gl.getUniformLocation(program, "u_time")
    const resolutionLocation = gl.getUniformLocation(program, "u_resolution")

    let animationFrameId: number
    const startTime = Date.now()

    const render = () => {
      const width = canvas.clientWidth
      const height = canvas.clientHeight
      
      // Resize canvas if needed
      if (canvas.width !== width || canvas.height !== height) {
        canvas.width = width
        canvas.height = height
        gl.viewport(0, 0, width, height)
      }

      gl.uniform2f(resolutionLocation, width, height)
      gl.uniform1f(timeLocation, (Date.now() - startTime) / 1000)

      gl.drawArrays(gl.TRIANGLES, 0, 6)
      animationFrameId = requestAnimationFrame(render)
    }

    render()

    return () => {
      cancelAnimationFrame(animationFrameId)
    }
  }, [])

  return (
    <canvas 
      ref={canvasRef} 
      className="absolute inset-0 w-full h-full"
      style={{ opacity: 0.8 }}
    />
  )
}
