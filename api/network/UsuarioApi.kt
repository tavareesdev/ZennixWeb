package com.zennix.zennix.network

import com.zennix.zennix.model.LoginRequest
import okhttp3.ResponseBody
import retrofit2.Response
import retrofit2.http.Body
import retrofit2.http.POST

interface UsuarioApi {
    // Ajuste o path conforme sua API real (ex: "api/auth/login" ou "usuario/login")
    @POST("login")
    suspend fun login(@Body request: LoginRequest): Response<ResponseBody>
}
