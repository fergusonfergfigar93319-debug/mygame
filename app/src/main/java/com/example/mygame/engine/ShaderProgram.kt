package com.example.mygame.engine

import android.opengl.GLES20

class ShaderProgram(vertexSrc: String, fragmentSrc: String) {

    val id: Int

    init {
        val vs = compileShader(GLES20.GL_VERTEX_SHADER, vertexSrc)
        val fs = compileShader(GLES20.GL_FRAGMENT_SHADER, fragmentSrc)
        id = GLES20.glCreateProgram()
        GLES20.glAttachShader(id, vs)
        GLES20.glAttachShader(id, fs)
        GLES20.glLinkProgram(id)
        GLES20.glDeleteShader(vs)
        GLES20.glDeleteShader(fs)
    }

    fun use() = GLES20.glUseProgram(id)
    fun getUniformLocation(name: String): Int = GLES20.glGetUniformLocation(id, name)
    fun getAttribLocation(name: String): Int = GLES20.glGetAttribLocation(id, name)
    fun dispose() = GLES20.glDeleteProgram(id)

    companion object {
        private fun compileShader(type: Int, src: String): Int {
            val shader = GLES20.glCreateShader(type)
            GLES20.glShaderSource(shader, src)
            GLES20.glCompileShader(shader)
            return shader
        }

        // Scene shader: vertex color + directional light + fog + emissive glow
        val SCENE_VERTEX = """
            uniform mat4 uMVP;
            uniform mat4 uModel;
            uniform float uEmissive;
            uniform float uFogNear;
            uniform float uFogFar;
            uniform float uSkyPass;
            uniform float uAuroraPass;
            uniform vec3 uSkyTint;
            uniform vec3 uAuroraTint;
            attribute vec4 aPosition;
            attribute vec4 aColor;
            varying vec4 vColor;
            varying float vFog;
            varying float vEmissive;
            void main() {
                vec4 worldPos = uModel * aPosition;
                gl_Position = uMVP * aPosition;
                if (uSkyPass > 0.5) {
                    vColor = vec4(aColor.rgb * uSkyTint, aColor.a);
                    vFog = 0.0;
                } else if (uAuroraPass > 0.5) {
                    vColor = vec4(aColor.rgb * uAuroraTint, aColor.a);
                    vFog = 0.0;
                } else {
                    vec3 normal = normalize(vec3(uModel[0][1], uModel[1][1], uModel[2][1]));
                    vec3 lightDir = normalize(vec3(-0.4, 0.8, -0.5));
                    float diff = max(dot(normal, lightDir), 0.0) * 0.45 + 0.55;
                    vColor = vec4(aColor.rgb * diff, aColor.a);
                    float eyeDist = -gl_Position.z / gl_Position.w;
                    vFog = clamp((eyeDist - uFogNear) / (uFogFar - uFogNear), 0.0, 1.0);
                }
                vEmissive = uEmissive;
            }
        """.trimIndent()

        val SCENE_FRAGMENT = """
            precision mediump float;
            uniform vec3 uFogColor;
            uniform float uAuroraPass;
            uniform float uAuroraStrength;
            varying vec4 vColor;
            varying float vFog;
            varying float vEmissive;
            void main() {
                if (uAuroraPass > 0.5) {
                    vec3 glow = vColor.rgb * vColor.a * uAuroraStrength;
                    gl_FragColor = vec4(glow, 1.0);
                    return;
                }
                vec3 emissiveColor = vColor.rgb + vec3(vEmissive * 0.4);
                vec3 fogged = mix(emissiveColor, uFogColor, vFog);
                gl_FragColor = vec4(fogged, vColor.a);
            }
        """.trimIndent()

        // Speed-line particle shader (additive blend, no fog)
        val PARTICLE_VERTEX = """
            uniform mat4 uMVP;
            attribute vec4 aPosition;
            attribute vec4 aColor;
            varying vec4 vColor;
            void main() {
                gl_Position = uMVP * aPosition;
                gl_PointSize = 4.0;
                vColor = aColor;
            }
        """.trimIndent()

        val PARTICLE_FRAGMENT = """
            precision mediump float;
            varying vec4 vColor;
            void main() {
                gl_FragColor = vColor;
            }
        """.trimIndent()
    }
}
