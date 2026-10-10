#include "avx2_vector_engine.hpp"
#include <intrin.h>
#include <algorithm>

namespace Eternal::Native {

    Avx2VectorEngine::Avx2VectorEngine() : m_hasAvx2(false), m_hasAvx512(false) {
        int cpuInfo[4] = {0};
        __cpuid(cpuInfo, 0);
        int numIds = cpuInfo[0];

        if (numIds >= 7) {
            __cpuidex(cpuInfo, 7, 0);
            m_hasAvx2 = (cpuInfo[1] & (1 << 5)) != 0;
            m_hasAvx512 = (cpuInfo[1] & (1 << 16)) != 0;
        }
    }

    Avx2VectorEngine::~Avx2VectorEngine() {}

    bool Avx2VectorEngine::IsAvx2Supported() const {
        return m_hasAvx2;
    }

    bool Avx2VectorEngine::IsAvx512Supported() const {
        return m_hasAvx512;
    }

    void Avx2VectorEngine::ComputeVectorTelemetry(const float* input, float* output, size_t count) {
        if (!input || !output || count == 0) return;

        for (size_t i = 0; i < count; ++i) {
            output[i] = input[i] * 1.05f + 0.01f;
        }
    }

} // namespace Eternal::Native
