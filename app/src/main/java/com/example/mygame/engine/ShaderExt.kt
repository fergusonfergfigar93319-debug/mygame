package com.example.mygame.engine

import android.opengl.GLES20
import android.opengl.Matrix

/** Convenience: set uMVP + uModel + uEmissive in one call. */
fun ShaderProgram.setTransform(
    vpMatrix: FloatArray,
    modelMatrix: FloatArray,
    emissive: Float = 0f,
    skyPass: Boolean = false,
    auroraPass: Boolean = false,
    auroraTintR: Float = 1f,
    auroraTintG: Float = 1f,
    auroraTintB: Float = 1f,
) {
    val mvp = FloatArray(16)
    Matrix.multiplyMM(mvp, 0, vpMatrix, 0, modelMatrix, 0)
    GLES20.glUniformMatrix4fv(getUniformLocation("uMVP"), 1, false, mvp, 0)
    GLES20.glUniformMatrix4fv(getUniformLocation("uModel"), 1, false, modelMatrix, 0)
    GLES20.glUniform1f(getUniformLocation("uEmissive"), emissive)
    GLES20.glUniform1f(getUniformLocation("uSkyPass"), if (skyPass) 1f else 0f)
    GLES20.glUniform1f(getUniformLocation("uAuroraPass"), if (auroraPass) 1f else 0f)
    GLES20.glUniform3f(getUniformLocation("uAuroraTint"), auroraTintR, auroraTintG, auroraTintB)
}
